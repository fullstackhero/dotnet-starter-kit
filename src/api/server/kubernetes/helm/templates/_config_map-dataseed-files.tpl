{{- define "maxiar-dotnetstarterkit-webapi-dataseed-files.config-map-contents" -}}
{{- $files := .Files }}
{{- range $index, $dataseed := .Values.dataConfiguration.dataSeed }}
  {{- (tpl ($files.Glob $dataseed.filePath).AsConfig $ ) | nindent 4 }}  
  {{- end }}
{{- end -}}
