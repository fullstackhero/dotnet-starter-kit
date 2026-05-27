################################################################################
# AWS WAFv2 Web ACL
################################################################################

resource "aws_wafv2_web_acl" "this" {
  name        = var.name
  description = var.description != "" ? var.description : "WAF for ${var.name}"
  scope       = "REGIONAL"

  default_action {
    allow {}
  }

  visibility_config {
    cloudwatch_metrics_enabled = true
    metric_name                = "${replace(var.name, "-", "")}WebAcl"
    sampled_requests_enabled   = true
  }

  ############################################################################
  # Rate Limiting Rule
  ############################################################################

  rule {
    name     = "RateLimit"
    priority = 1

    action {
      block {}
    }

    statement {
      rate_based_statement {
        limit              = var.rate_limit
        aggregate_key_type = "IP"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${replace(var.name, "-", "")}RateLimit"
      sampled_requests_enabled   = true
    }
  }

  ############################################################################
  # AWS Managed Rules - Common Rule Set
  ############################################################################

  rule {
    name     = "AWSManagedRulesCommonRuleSet"
    priority = 10

    override_action {
      none {}
    }

    statement {
      managed_rule_group_statement {
        name        = "AWSManagedRulesCommonRuleSet"
        vendor_name = "AWS"

        dynamic "rule_action_override" {
          for_each = var.common_ruleset_excluded_rules
          content {
            name = rule_action_override.value
            action_to_use {
              count {}
            }
          }
        }
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${replace(var.name, "-", "")}CommonRuleSet"
      sampled_requests_enabled   = true
    }
  }

  ############################################################################
  # AWS Managed Rules - Known Bad Inputs
  ############################################################################

  rule {
    name     = "AWSManagedRulesKnownBadInputsRuleSet"
    priority = 20

    override_action {
      none {}
    }

    statement {
      managed_rule_group_statement {
        name        = "AWSManagedRulesKnownBadInputsRuleSet"
        vendor_name = "AWS"
      }
    }

    visibility_config {
      cloudwatch_metrics_enabled = true
      metric_name                = "${replace(var.name, "-", "")}KnownBadInputs"
      sampled_requests_enabled   = true
    }
  }

  ############################################################################
  # AWS Managed Rules - SQL Injection
  ############################################################################

  dynamic "rule" {
    for_each = var.enable_sqli_rule_set ? [1] : []
    content {
      name     = "AWSManagedRulesSQLiRuleSet"
      priority = 30

      override_action {
        none {}
      }

      statement {
        managed_rule_group_statement {
          name        = "AWSManagedRulesSQLiRuleSet"
          vendor_name = "AWS"
        }
      }

      visibility_config {
        cloudwatch_metrics_enabled = true
        metric_name                = "${replace(var.name, "-", "")}SQLiRuleSet"
        sampled_requests_enabled   = true
      }
    }
  }

  ############################################################################
  # AWS Managed Rules - IP Reputation
  ############################################################################

  dynamic "rule" {
    for_each = var.enable_ip_reputation_rule_set ? [1] : []
    content {
      name     = "AWSManagedRulesAmazonIpReputationList"
      priority = 40

      override_action {
        none {}
      }

      statement {
        managed_rule_group_statement {
          name        = "AWSManagedRulesAmazonIpReputationList"
          vendor_name = "AWS"
        }
      }

      visibility_config {
        cloudwatch_metrics_enabled = true
        metric_name                = "${replace(var.name, "-", "")}IpReputation"
        sampled_requests_enabled   = true
      }
    }
  }

  ############################################################################
  # AWS Managed Rules - Anonymous IP List
  ############################################################################

  dynamic "rule" {
    for_each = var.enable_anonymous_ip_rule_set ? [1] : []
    content {
      name     = "AWSManagedRulesAnonymousIpList"
      priority = 50

      override_action {
        none {}
      }

      statement {
        managed_rule_group_statement {
          name        = "AWSManagedRulesAnonymousIpList"
          vendor_name = "AWS"
        }
      }

      visibility_config {
        cloudwatch_metrics_enabled = true
        metric_name                = "${replace(var.name, "-", "")}AnonymousIp"
        sampled_requests_enabled   = true
      }
    }
  }

  ############################################################################
  # AWS Managed Rules - Linux OS
  ############################################################################

  dynamic "rule" {
    for_each = var.enable_linux_rule_set ? [1] : []
    content {
      name     = "AWSManagedRulesLinuxRuleSet"
      priority = 60

      override_action {
        none {}
      }

      statement {
        managed_rule_group_statement {
          name        = "AWSManagedRulesLinuxRuleSet"
          vendor_name = "AWS"
        }
      }

      visibility_config {
        cloudwatch_metrics_enabled = true
        metric_name                = "${replace(var.name, "-", "")}LinuxRuleSet"
        sampled_requests_enabled   = true
      }
    }
  }

  tags = var.tags
}

################################################################################
# WAF Association with ALB
################################################################################

resource "aws_wafv2_web_acl_association" "this" {
  count = var.alb_arn != null ? 1 : 0

  resource_arn = var.alb_arn
  web_acl_arn  = aws_wafv2_web_acl.this.arn
}

################################################################################
# WAF Logging (Optional)
################################################################################

resource "aws_cloudwatch_log_group" "waf" {
  count = var.enable_logging ? 1 : 0

  # WAF logging requires log group name to start with aws-waf-logs-
  name              = "aws-waf-logs-${var.name}"
  retention_in_days = var.log_retention_in_days
  kms_key_id        = var.kms_key_id

  tags = var.tags
}

resource "aws_wafv2_web_acl_logging_configuration" "this" {
  count = var.enable_logging ? 1 : 0

  log_destination_configs = [aws_cloudwatch_log_group.waf[0].arn]
  resource_arn            = aws_wafv2_web_acl.this.arn

  dynamic "redacted_fields" {
    for_each = var.redacted_fields
    content {
      dynamic "single_header" {
        for_each = redacted_fields.value.type == "single_header" ? [1] : []
        content {
          name = redacted_fields.value.name
        }
      }
    }
  }
}
