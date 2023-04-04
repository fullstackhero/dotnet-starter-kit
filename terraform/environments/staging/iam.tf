resource "aws_iam_role" "ecs_task_execution_role" {
  name               = "fsh-ecs-task-execution-role"
  tags               = merge(var.common_tags)
  assume_role_policy = <<EOF
{
 "Version": "2012-10-17",
 "Statement": [
   {
     "Action": "sts:AssumeRole",
     "Principal": {
       "Service": "ecs-tasks.amazonaws.com"
     },
     "Effect": "Allow",
     "Sid": ""
   }
 ]
}
EOF
}
resource "aws_iam_role" "ecs_task_role" {
  name               = "fsh-ecs-task-role"
  tags               = merge(var.common_tags)
  assume_role_policy = <<EOF
{
 "Version": "2012-10-17",
 "Statement": [
   {
     "Action": "sts:AssumeRole",
     "Principal": {
       "Service": "ecs-tasks.amazonaws.com"
     },
     "Effect": "Allow",
     "Sid": ""
   }
 ]
}
EOF
}

resource "aws_iam_role_policy_attachment" "ecs-task-execution-role-policy-attachment" {
  role       = aws_iam_role.ecs_task_execution_role.name
  policy_arn = "arn:aws:iam::aws:policy/service-role/AmazonECSTaskExecutionRolePolicy"
}

resource "aws_iam_role_policy" "cloudwatch_policy" {
  role   = aws_iam_role.ecs_task_role.name
  policy = <<EOF
{
 "Version": "2012-10-17",
 "Statement": [
   {
      "Sid": "LogStreams",
      "Effect": "Allow",
      "Action": [
          "logs:CreateLogStream",
          "logs:DescribeLogStreams",
          "logs:PutLogEvents"
      ],
      "Resource": "arn:aws:logs:*:*:log-group:fsh/dotnet-webapi:log-stream:*"
    },
    {
      "Sid": "LogGroups",
      "Effect": "Allow",
      "Action": [
          "logs:DescribeLogGroups"
      ],
      "Resource": "arn:aws:logs:*:*:log-group:fsh/dotnet-webapi"
    }
 ]
}
EOF
}
