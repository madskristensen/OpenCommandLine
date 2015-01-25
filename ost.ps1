# VSIX Module for AppVeyor by Mads Kristensen

function Vsix-Build {

    [CmdletBinding()]
    param (
        [Parameter(Position=0, Mandatory=0, ValueFromPipelineByPropertyName=1)]
        [string]$project = "*.sln",

        [Parameter(Position=1, Mandatory=0, ValueFromPipelineByPropertyName=1)]
        [string]$configuration = "Release"
    ) 

     $buildFile = Get-ChildItem $project
     $env:CONFIGURATION = $configuration

     Write-Host "Building" $buildFile.Name"..." -ForegroundColor Cyan -NoNewline
     #msbuild $project.FullName /p:configuration=$configuration /p:DeployExtension=false /p:ZipPackageCompressionLevel=normal /v:m
     Write-Host "OK" -ForegroundColor Green
}


function Vsix-PushArtifacts {

    [CmdletBinding()]
    param (
        [Parameter(Position=0, Mandatory=0, ValueFromPipeline=1, ValueFromPipelineByPropertyName=1)]
        [string]$configuration = $env:CONFIGURATION,

        [Parameter(Position=1, Mandatory=0, ValueFromPipeline=1, ValueFromPipelineByPropertyName=1)]
        [string]$p = $env:CONFIGURATION
    )

     
   $fileName = (Get-ChildItem "./**/bin/$configuration/*.vsix")
     
        Write-Host "Pushing artifact" $fileName.Name"..." -ForegroundColor Cyan -NoNewline
        #Push-AppveyorArtifact $fileName.FullName -FileName $fileName.Name
        Write-Host "OK" -ForegroundColor Green
}

function Vsix-UpdateBuildVersion {

    [CmdletBinding()]
    param (
        [Parameter(Position=0, Mandatory=0, ValueFromPipelineByPropertyName=1)]
        [Version]$version = $env:APPVEYOR_BUILD_VERSION
    )

    process{
    
    Write-Host "Updating AppVeyor build version..." -ForegroundColor Cyan -NoNewline
    #Update-AppveyorBuild -Version $version.ToString()
    Write-Host $version.ToString() -ForegroundColor Green
    }
    
}

function Vsix-IncrementVersion {

    [CmdletBinding()]
    param (
        [Parameter(Position=0, Mandatory=0)]
        [string]$manifestFilePath = "**\source.extension.vsixmanifest",

        [Parameter(Position=1, Mandatory=0)]
        [int]$buildNumber = $env:APPVEYOR_BUILD_NUMBER,

        [ValidateSet("build","revision")]
        [Parameter(Position=2, Mandatory=0)]
        [string]$versionType = "build"
    )

    Write-Host "`nIncrementing VSIX version..."  -ForegroundColor Cyan -NoNewline

    $vsixManifest = Get-ChildItem $manifestFilePath
    [xml]$vsixXml = Get-Content $vsixManifest

    $ns = New-Object System.Xml.XmlNamespaceManager $vsixXml.NameTable
    $ns.AddNamespace("ns", $vsixXml.DocumentElement.NamespaceURI)

    $attrVersion = $vsixXml.SelectSingleNode("//ns:Identity", $ns).Attributes["Version"]

    [Version]$version = $attrVersion.Value;

    if ($versionType -eq "build"){
        $version = New-Object Version ([int]$version.Major),([int]$version.Minor),$buildNumber
    }
    elseif ($versionType -eq "revision"){
        $version = New-Object Version ([int]$version.Major),([int]$version.Minor),([System.Math]::Max([int]$version.Build, 0)),$buildNumber
    }
    
    $attrVersion.Value = $version

    $vsixXml.Save($vsixManifest)

    $env:APPVEYOR_BUILD_VERSION = $version.ToString()

    Write-Host $version.ToString() -ForegroundColor Green
}