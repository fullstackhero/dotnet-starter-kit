resource "aws_internet_gateway" "demo-vpc-internet-gateway" {
  vpc_id = "${aws_vpc.demo-vpc.id}"
}

resource "aws_network_acl" "demo-vpc-network-acl" {
    vpc_id = "${aws_vpc.demo-vpc.id}"
    subnet_ids = ["${aws_subnet.demo-vpc-subnet1.id}", "${aws_subnet.demo-vpc-subnet2.id}"]

    egress {
        protocol   = "-1"
        rule_no    = 100
        action     = "allow"
        cidr_block = "0.0.0.0/0"
        from_port  = 0
        to_port    = 0
    }

    ingress {
        protocol   = "-1"
        rule_no    = 100
        action     = "allow"
        cidr_block = "0.0.0.0/0"
        from_port  = 0
        to_port    = 0
    }
}