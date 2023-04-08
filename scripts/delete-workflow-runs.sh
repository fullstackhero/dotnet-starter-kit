org=fullstackhero
repo=dotnet-webapi-boilerplate
workflow_id=12524751 #add in your workflow id here.
echo "Listing runs for the workflow ID $workflow_id"
run_ids=( $(gh api repos/$org/$repo/actions/workflows/$workflow_id/runs --paginate | jq '.workflow_runs[].id') )
for run_id in "${run_ids[@]}"
do
echo "Deleting Run ID $run_id"
gh api repos/$org/$repo/actions/runs/${run_id%$'\r'} --method DELETE -H "Accept: application/vnd.github+json" -H "X-GitHub-Api-Version: 2022-11-28"
done

read -p "Press any key..."