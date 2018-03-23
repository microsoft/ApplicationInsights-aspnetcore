 $sdkcpu = (Get-Counter -Counter "\process(dotnet#1)\% processor Time" -SampleInterval 1 -MaxSamples 30 |
        Select-Object -ExpandProperty CounterSamples |
        Select-Object -ExpandProperty CookedValue |
        Measure-Object -Average).Average

#Add-Content "c:\perftests\cijo.txt" "hello $sdkcpu"

Write-Host "$sdkcpu"