# Configuring Backend For Terraform

We'll be using AWS S3 as the backed for terraform and store the state file locks within a DynamoDB Table. 

Note that you need to run at this directory first.

- `terraform init`
- `terraform plan`
- `terraform apply`

Once this is done, your terraform backend infrastructure would be ready. 

Next navigate to /environments/dev and initialize your terraform!