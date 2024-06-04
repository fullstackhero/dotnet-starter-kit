resource "aws_s3_bucket" "s3_bucket" {
  bucket = "fullstackhero-terraform-backend"
  tags = {
    Name    = "fullstackhero-terraform-backend"
    Project = "fullstackhero"
  }
  lifecycle {
    prevent_destroy = true
  }
}
