Add-Type -AssemblyName System.Drawing

function Optimize-Image {
    param(
        [string]$Path,
        [int]$Width,
        [int]$Height,
        [int]$Quality
    )
    $src = [System.Drawing.Image]::FromFile($Path)
    $bmp = New-Object System.Drawing.Bitmap($Width, $Height)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.DrawImage($src, 0, 0, $Width, $Height)
    
    $codecs = [System.Drawing.Imaging.ImageCodecInfo]::GetImageEncoders()
    $jpgCodec = $codecs | Where-Object { $_.MimeType -eq "image/jpeg" }
    
    $encoderParams = New-Object System.Drawing.Imaging.EncoderParameters(1)
    $encoderParams.Param[0] = New-Object System.Drawing.Imaging.EncoderParameter([System.Drawing.Imaging.Encoder]::Quality, $Quality)
    
    $tempPath = $Path + ".tmp"
    $bmp.Save($tempPath, $jpgCodec, $encoderParams)
    
    $g.Dispose()
    $bmp.Dispose()
    $src.Dispose()
    
    Remove-Item $Path -Force
    Move-Item $tempPath $Path -Force
}

Optimize-Image -Path "src/assets/app_logo.jpg" -Width 128 -Height 128 -Quality 75
Optimize-Image -Path "src/assets/dashboard_welcome.jpg" -Width 128 -Height 128 -Quality 75
Optimize-Image -Path "src/assets/success_checkout.jpg" -Width 128 -Height 128 -Quality 75
Optimize-Image -Path "src/assets/offline_state.jpg" -Width 256 -Height 256 -Quality 75
Optimize-Image -Path "src/assets/login_hero.jpg" -Width 800 -Height 800 -Quality 70
