param(
    [string]$crawlUrlBase='https://apod.nasa.gov/apod',
    [string]$crawlArchivePath='/archivepix.html',
    [string]$linkHrefFilter="ap*",
    [string]$destination=(Join-Path $env:USERPROFILE 'Scratch\APOD_html')
    
    )

New-Item -ItemType Directory -Force -Path $destination
$archiveUrl = "${crawlUrlBase}${crawlArchivePath}"
[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
$apodList = ((Invoke-WebRequest -Uri $archiveUrl).Links | Where href -like $linkHrefFilter).href

Foreach ($apod in $apodList) {
    $sourceUrl = "${crawlUrlBase}/${apod}"
    $destAddr = (Join-Path ${destination} ${apod})
    if (-Not ([System.IO.File]::Exists($destAddr))) {
        echo "Getting $sourceUrl"
        & wget ($sourceUrl) -O ($destAddr)
        #Start-BitsTransfer -Source ($sourceUrl) -Destination ($destUrl) # fails on dynamic web pages, use wget instead
        Start-Sleep -Milliseconds 200
    }
}
