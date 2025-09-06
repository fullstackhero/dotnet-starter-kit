terraform {
  backend "s3" {
    bucket         = "fullstackhero-terraform-backend"
    key            = "fullstackhero/dev/terraform.tfstate"
    region         = "us-east-1"
    dynamodb_table = "fullstackhero-state-locks"
  }
}
