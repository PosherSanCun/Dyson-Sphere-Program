param(
    [Parameter(Mandatory = $true)]
    [string]$GameRoot
)

$ErrorActionPreference = 'Stop'

# 在独立 PowerShell 进程内运行，只构造内存中的原生存储对象，不连接游戏或读写存档。
$managed = Join-Path $GameRoot 'DSPGAME_Data\Managed'
$null = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'UnityEngine.CoreModule.dll'))
$gameAssembly = [Reflection.Assembly]::LoadFrom((Join-Path $managed 'Assembly-CSharp.dll'))
$storageType = $gameAssembly.GetType('StorageComponent', $true)
$storageKindType = $gameAssembly.GetType('EStorageType', $true)
$transferMethod = $storageType.GetMethod('TransferTo', [type[]]@($storageType))
if ($null -eq $transferMethod) {
    throw '当前游戏缺少 StorageComponent.TransferTo(StorageComponent)。'
}

# 构造函数默认 LoadStatic 会读取 LDB；为隔离检查手工提供物品缓存，结束后恢复。
$savedStatics = @{}
$staticNames = @('staticLoaded', 'itemStackCount', 'itemIsFuel', 'itemIsAmmo', 'itemIsBomb', 'itemIsFighter')
foreach ($fieldName in $staticNames) {
    $field = $storageType.GetField($fieldName)
    if ($null -eq $field) { throw "当前游戏缺少原生字段：$fieldName" }
    $savedStatics[$fieldName] = $field.GetValue($null)
}

function New-TestStorage([int]$Size, [int]$Kind, [bool]$PlayerInventory) {
    $storage = [Activator]::CreateInstance($storageType, [object[]]@($Size))
    $storageType.GetField('type').SetValue($storage, [Enum]::ToObject($storageKindType, $Kind))
    $storageType.GetField('isPlayerInventory').SetValue($storage, $PlayerInventory)
    return $storage
}

function Set-TestGrid($Storage, [int]$Index, [int]$Item, [int]$Count, [int]$Inc,
                      [int]$Filter, [int]$Capacity) {
    $grids = $storageType.GetField('grids').GetValue($Storage)
    $grid = $grids.GetValue($Index)
    $values = @{ itemId = $Item; count = $Count; inc = $Inc; filter = $Filter; stackSize = $Capacity }
    foreach ($key in $values.Keys) {
        $grid.GetType().GetField($key).SetValue($grid, $values[$key])
    }
    $grids.SetValue($grid, $Index)
}

function Get-GridState($Storage) {
    $parts = @(foreach ($grid in $storageType.GetField('grids').GetValue($Storage)) {
        '{0}:{1}:{2}:{3}:{4}' -f $grid.itemId, $grid.count, $grid.inc, $grid.filter, $grid.stackSize
    })
    return ($parts -join '|')
}

function Get-Filters($Storage) {
    return ((@($storageType.GetField('grids').GetValue($Storage) | ForEach-Object { $_.filter })) -join ',')
}

function Get-TotalCount($Storage) {
    [long]$total = 0
    foreach ($grid in $storageType.GetField('grids').GetValue($Storage)) { $total += $grid.count }
    return $total
}

function Get-CombinedItemTotals($Source, $Destination) {
    $totals = @{}
    foreach ($storage in @($Source, $Destination)) {
        foreach ($grid in $storageType.GetField('grids').GetValue($storage)) {
            if ($grid.itemId -le 0 -or $grid.count -le 0) { continue }
            $key = [string]$grid.itemId
            if (-not $totals.ContainsKey($key)) { $totals[$key] = [long[]]@(0, 0) }
            $totals[$key][0] += $grid.count
            $totals[$key][1] += $grid.inc
        }
    }
    return ((@(foreach ($key in ($totals.Keys | Sort-Object)) {
        '{0}:{1}:{2}' -f $key, $totals[$key][0], $totals[$key][1]
    })) -join '|')
}

try {
    $stackCounts = [int[]]::new(12000)
    for ($i = 0; $i -lt $stackCounts.Length; $i++) { $stackCounts[$i] = 100 }
    $storageType.GetField('itemStackCount').SetValue($null, $stackCounts)
    foreach ($fieldName in @('itemIsFuel', 'itemIsAmmo', 'itemIsBomb', 'itemIsFighter')) {
        $storageType.GetField($fieldName).SetValue($null, [bool[]]::new(12000))
    }
    $storageType.GetField('staticLoaded').SetValue($null, $true)

    $results = @()
    $cases = @('full', 'partial', 'unconfigured', 'multiple-source',
               'duplicate-destination', 'filter-preserved', 'full-destination', 'empty-source')
    foreach ($caseName in $cases) {
        # 默认背包为 type=0；物流界面使用 type=8 的有过滤器代理仓。
        $source = New-TestStorage 4 0 $true
        $destination = New-TestStorage 4 8 $false
        $expectedMoved = 0
        switch ($caseName) {
            'full' {
                Set-TestGrid $source 0 1001 80 320 0 100
                Set-TestGrid $destination 0 1001 10 40 1001 100
                $expectedMoved = 80
            }
            'partial' {
                Set-TestGrid $source 0 1001 70 215 0 100
                Set-TestGrid $destination 0 1001 90 270 1001 100
                $expectedMoved = 10
            }
            'unconfigured' {
                Set-TestGrid $source 0 1002 30 120 0 100
                Set-TestGrid $destination 0 1001 10 40 1001 100
            }
            'multiple-source' {
                Set-TestGrid $source 0 1001 60 95 0 100
                Set-TestGrid $source 1 1001 50 155 0 100
                Set-TestGrid $source 2 1002 20 0 0 100
                Set-TestGrid $destination 0 1001 0 0 1001 95
                $expectedMoved = 95
            }
            'duplicate-destination' {
                # 原版正常界面禁止重复物流物品；异常重复配置只应使用首格，不能丢失余量。
                Set-TestGrid $source 0 1001 50 173 0 100
                Set-TestGrid $destination 0 1001 90 270 1001 100
                Set-TestGrid $destination 1 1001 0 0 1001 100
                $expectedMoved = 10
            }
            'filter-preserved' {
                Set-TestGrid $source 0 1001 35 97 1001 100
                Set-TestGrid $source 1 1002 18 60 1002 100
                Set-TestGrid $destination 0 1001 0 0 1001 100
                Set-TestGrid $destination 1 1002 0 0 1002 100
                $expectedMoved = 53
            }
            'full-destination' {
                Set-TestGrid $source 0 1001 35 97 0 100
                Set-TestGrid $destination 0 1001 100 400 1001 100
            }
            'empty-source' {
                Set-TestGrid $source 0 1001 0 0 1001 100
                Set-TestGrid $destination 0 1001 20 63 1001 100
            }
        }

        $totalsBefore = Get-CombinedItemTotals $source $destination
        $sourceFilters = Get-Filters $source
        $destinationFilters = Get-Filters $destination
        $sourceBefore = Get-GridState $source
        $destinationBefore = Get-GridState $destination
        $destinationCountBefore = Get-TotalCount $destination
        $emptyDestinationIndices = @()
        $destinationGrids = $storageType.GetField('grids').GetValue($destination)
        for ($index = 0; $index -lt $destinationGrids.Length; $index++) {
            if ($destinationGrids[$index].filter -eq 0) { $emptyDestinationIndices += $index }
        }

        # 调用真实游戏方法；非复制/镜像模组自己的业务实现。
        $null = $transferMethod.Invoke($source, [object[]]@($destination))
        $moved = (Get-TotalCount $destination) - $destinationCountBefore
        if ($moved -ne $expectedMoved) { throw "$caseName 转入数错误：$moved / $expectedMoved" }
        if ((Get-CombinedItemTotals $source $destination) -ne $totalsBefore) {
            throw "$caseName 每种物品的数量或 inc 不守恒。"
        }
        if ((Get-Filters $source) -ne $sourceFilters -or (Get-Filters $destination) -ne $destinationFilters) {
            throw "$caseName 改变了源或目的格的物品过滤设置。"
        }
        foreach ($index in $emptyDestinationIndices) {
            $grid = $destinationGrids[$index]
            if ($grid.itemId -ne 0 -or $grid.count -ne 0 -or $grid.inc -ne 0 -or $grid.filter -ne 0) {
                throw "$caseName 向未配置的物流格写入了物品。"
            }
        }
        if ($caseName -eq 'duplicate-destination' -and $destinationGrids[1].count -ne 0) {
            throw '异常重复目的格行为与当前原生规则不符。'
        }
        if ($expectedMoved -eq 0 -and
            ((Get-GridState $source) -ne $sourceBefore -or (Get-GridState $destination) -ne $destinationBefore)) {
            throw "$caseName 不应发生变化时改变了格内容。"
        }

        $once = (Get-GridState $source) + '/' + (Get-GridState $destination)
        $null = $transferMethod.Invoke($source, [object[]]@($destination))
        if (((Get-GridState $source) + '/' + (Get-GridState $destination)) -ne $once) {
            throw "$caseName 第二次调用不应继续改变内容。"
        }
        $results += [pscustomobject]@{
            Case = $caseName
            Moved = $moved
            ItemCountAndIncConserved = $true
            FiltersUnchanged = $true
            UnconfiguredSlotsUnchanged = $true
            RepeatNoChange = $true
        }
    }
    $results | ConvertTo-Json -Depth 3
    Write-Output ('原生存储检查通过：{0} / {0}' -f $results.Count)
}
finally {
    foreach ($fieldName in $staticNames) {
        $storageType.GetField($fieldName).SetValue($null, $savedStatics[$fieldName])
    }
}
