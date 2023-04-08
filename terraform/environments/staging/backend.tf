terraform {
  backend "s3" {
    bucket         = "fsh-backend"
    key            = "api/staging/terraform.tfstate"
    region         = "ap-south-1"
    dynamodb_table = "fsh-state-locks"
  }
}
