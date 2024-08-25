{{- define "maxiar-dotnetstarterkit-blazor-logging-files.config-map-contents" -}}
  envsubst_on_yamls.sh: |
    #!/bin/sh
    set -e
    exec 3>&1
    ME=$(basename $0)
    auto_envsubst() {
      local template_dir="${KTCVAR_Security_IdentityJson_TEMPLATE_DIR:-/config-tpl}"
      local suffix="${KTCVAR_Security_IdentityJson_TEMPLATE_SUFFIX:-.template}"
      local output_dir="${KTCVAR_Security_IdentityJson_OUTPUT_DIR:-/usr/share/filebeat}"
      echo "template_dir is [$template_dir]"
      echo "suffix is [$suffix]"
      echo "output_dir is [$output_dir]"
      local template defined_envs relative_path output_path subdir
      defined_envs=$(printf '${%s} ' $(env | cut -d= -f1))
      [ -d "$template_dir" ] || return 0
      if [ ! -w "$output_dir" ]; then
        echo >&3 "$ME: ERROR: $template_dir exists, but $output_dir is not writable"
        return 0
      fi
      find "$template_dir" -follow -type f -name "*$suffix" -print | while read -r template; do
        relative_path="${template#$template_dir/}"
        output_path="$output_dir/${relative_path%$suffix}"
        subdir=$(dirname "$relative_path")
        # create a subdirectory where the template file exists
        mkdir -p "$output_dir/$subdir"
        echo >&3 "$ME: Running envsubst on $template to $output_path"
        envsubst "$defined_envs" < "$template" > "$output_path"
      done
    }
    auto_envsubst
    exit 0
  filebeat.yml: |
    filebeat:
      config:
        modules:
          path: /usr/share/filebeat/modules.d/*.yml
          reload:
            enabled: true
      modules:
      - module: nginx
        access:
          var.paths: ["/var/log/nginx/access.log*"]
        error:
          var.paths: ["/var/log/nginx/error.log*"]
    output:
      elasticsearch:
        hosts: ["${KTCVAR_Frontend_Global__ElasticHost}"]

{{- end }}