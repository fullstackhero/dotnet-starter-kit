terraform {
  backend "s3" {
    bucket         = "fullstackhero-terraform"
    key            = "fullstackhero/staging/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "fullstackhero-state-locks"
  }
}
