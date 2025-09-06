variable "cidr_block" {
  type    = string
  default = "10.0.0.0/16"
}

variable "private_cidr_a" {
  type    = string
  default = "10.0.1.0/24"
}

variable "private_cidr_b" {
  type    = string
  default = "10.0.2.0/24"
}

variable "availability_zone_a" {
  type    = string
  default = "us-east-1a"
}
variable "availability_zone_b" {
  type    = string
  default = "us-east-1b"
}
