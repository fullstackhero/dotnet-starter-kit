################################################################################
# Provider
#
# default_tags are applied to every taggable resource created by this provider
# (including those inside child modules), so individual resources only add a
# Name tag. See locals in main.tf.
################################################################################

provider "aws" {
  region = var.region

  default_tags {
    tags = merge(
      {
        Environment = var.environment
        Project     = "dotnet-starter-kit"
        ManagedBy   = "terraform"
      },
      var.owner != null ? { Owner = var.owner } : {}
    )
  }
}
