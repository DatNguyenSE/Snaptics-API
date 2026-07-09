Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "🚀 BẮT ĐẦU QUÁ TRÌNH DEPLOY SNAPCTICS-API" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan

# 1. Build
Write-Host "`n[1/5] Đang gói code mới vào Docker..." -ForegroundColor Yellow
docker build -t snaptics-api .

# 2. Tag
Write-Host "`n[2/5] Đang dán nhãn cho gói code..." -ForegroundColor Yellow
docker tag snaptics-api:latest 923988301802.dkr.ecr.ap-southeast-1.amazonaws.com/snaptics-api:latest

# 3. Login (Gộp luôn bước này để không bao giờ bị lỗi hết hạn 12 tiếng)
Write-Host "`n[3/5] Đang lấy chìa khóa vào cổng AWS..." -ForegroundColor Yellow
$token = aws ecr get-login-password --region ap-southeast-1
docker login --username AWS --password $token 923988301802.dkr.ecr.ap-southeast-1.amazonaws.com

# 4. Push
Write-Host "`n[4/5] Đang tải code lên mây (AWS ECR)..." -ForegroundColor Yellow
docker push 923988301802.dkr.ecr.ap-southeast-1.amazonaws.com/snaptics-api:latest

# 5. Khởi động lại Server
Write-Host "`n[5/5] Đang ra lệnh cho AWS đổi nhân viên mới (Force New Deployment)..." -ForegroundColor Yellow
aws ecs update-service --cluster Snaptics-Cluster --service snaptics-api-service --force-new-deployment --region ap-southeast-1 | Out-Null

Write-Host "`n==========================================" -ForegroundColor Green
Write-Host "🎉 ĐÃ GỬI LỆNH DEPLOY THÀNH CÔNG!" -ForegroundColor Green
Write-Host "Hệ thống AWS đang tự động thay thế máy chủ ngầm bên dưới." -ForegroundColor White
Write-Host "Khoảng 1-2 phút nữa bạn lên giao diện ECS lấy Public IP mới nhé!" -ForegroundColor White
Write-Host "==========================================" -ForegroundColor Green
