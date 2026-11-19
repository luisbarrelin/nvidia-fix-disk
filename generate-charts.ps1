using namespace System.Drawing
using namespace System.Windows.Forms.DataVisualization.Charting

[CmdletBinding()]
param()

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Windows.Forms.DataVisualization
[System.Threading.Thread]::CurrentThread.CurrentCulture = [System.Globalization.CultureInfo]::InvariantCulture

$scenarios = @(
    @{ Rate = 3.0; Label = '3 GB/h - community estimate'; Color = '#21735B'; Dash = 'Dot' },
    @{ Rate = 7.4; Label = '7.4 GB/h - whole-disk sample'; Color = '#205EAC'; Dash = 'Dash' },
    @{ Rate = 30.0; Label = '30 GB/h - community estimate'; Color = '#B14F26'; Dash = 'Solid' }
)

function Write-ProjectionChart {
    param($File, $Heading, $AxisLabel, $MaxHours, $TickHours, $MaxY, $TickY, $Scale)

    $chart = [Chart]::new()
    try {
        $chart.Width = 1000
        $chart.Height = 460
        $chart.BackColor = [Color]::White

        $title = $chart.Titles.Add($Heading)
        $title.Font = [Font]::new('Segoe UI', 19, [FontStyle]::Bold)
        $title.ForeColor = [ColorTranslator]::FromHtml('#18293C')

        $area = $chart.ChartAreas.Add('Projection')
        $area.AxisX.Minimum = 0
        $area.AxisX.Maximum = $MaxHours
        $area.AxisX.Interval = $TickHours
        $area.AxisX.Title = 'Hours at a constant write rate'
        $area.AxisX.MajorGrid.Enabled = $false
        $area.AxisY.Minimum = 0
        $area.AxisY.Maximum = $MaxY
        $area.AxisY.Interval = $TickY
        $area.AxisY.Title = $AxisLabel
        $area.AxisY.MajorGrid.LineColor = [ColorTranslator]::FromHtml('#DCE4EC')
        foreach ($axis in @($area.AxisX, $area.AxisY)) {
            $axis.LabelStyle.Font = [Font]::new('Segoe UI', 10)
            $axis.TitleFont = [Font]::new('Segoe UI', 11)
            $axis.LineColor = [ColorTranslator]::FromHtml('#64748B')
        }

        $legend = $chart.Legends.Add('Rates')
        $legend.Docking = [Docking]::Bottom
        $legend.Alignment = [StringAlignment]::Center
        $legend.Font = [Font]::new('Segoe UI', 10)

        foreach ($scenario in $scenarios) {
            $line = $chart.Series.Add($scenario.Label)
            $line.ChartArea = $area.Name
            $line.Legend = $legend.Name
            $line.ChartType = [SeriesChartType]::Line
            $line.BorderWidth = 4
            $line.BorderDashStyle = [ChartDashStyle]$scenario.Dash
            $line.Color = [ColorTranslator]::FromHtml($scenario.Color)
            foreach ($hour in @(0, $MaxHours)) {
                [void]$line.Points.AddXY($hour, ($hour * $scenario.Rate * $Scale))
            }
        }

        $chart.SaveImage((Join-Path $PSScriptRoot $File), [ChartImageFormat]::Png)
    }
    finally {
        $chart.Dispose()
    }
}

Write-ProjectionChart 'ssd-writes-24h.png' 'Projected SSD writes over 24 hours' 'Cumulative host writes (GB)' 24 4 750 150 1
Write-ProjectionChart 'ssd-tbw-168h.png' 'Projected share of a 600 TBW rating over 7 days' 'Share of rated TBW (%)' 168 24 1.0 0.2 (100.0 / 600000)
