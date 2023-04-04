resource "aws_s3_bucket" "s3_bucket" {
  bucket = "fsh-backend"
  tags = {
    Name = "fsh-backend"
  }
  lifecycle {
    prevent_destroy = true
  }
}
