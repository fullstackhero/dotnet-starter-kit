namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class DeleteEntityRequest
{

    public DeleteEntityRequest(string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, string _usingpathtochildren, string _readrepositoryLines, string _publicrepositoryLine, string _repo_Repo, string _repoRepo, Dictionary<string, string> fks)
    {
        string basicsources = pathtobasicsources + "DeleteEntityRequest.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string entitytolower = entity.ToLower();
        string deleteentityrequest = string.Empty;
        string asyncsources = pathtobasicsources + "DeleteEntityRequestAsyncTask.txt";
        string startchild = string.Empty;

        foreach (var key in fks)
        {
            string childEntity = key.Key;
            string childEntityPlural = key.Value;

            string child = File.ReadAllText(asyncsources)
                 .Replace("<&ChildEntity&>", childEntity.ToLower())
                 .Replace("<&ChildEntityPLural&>", childEntityPlural)
                 .Replace("<&Entity&>", entity)
                 .Replace("<&EntityToLower&>", entitytolower);
            startchild = startchild + child + Environment.NewLine;
        }

        deleteentityrequest = File.ReadAllText(basicsources)
       .Replace("<&usingpathtochildren&>", string.IsNullOrEmpty(_usingpathtochildren) ? string.Empty : _usingpathtochildren)
       .Replace("<&StringNameSpace&>", thenamespace)
       .Replace("<&Entity&>", entity)
       .Replace("<&EntityToLower&>", entitytolower)
        .Replace("<&repo_Repo&>", string.IsNullOrEmpty(_repo_Repo) ? string.Empty : _repo_Repo)
        .Replace("<&repoRepo&>", string.IsNullOrEmpty(_repoRepo) ? string.Empty : _repoRepo)
        .Replace("<&ReadRepositoryLines&>", string.IsNullOrEmpty(_readrepositoryLines) ? string.Empty : _readrepositoryLines)
        .Replace("<&PublicRepositoryLine&>", string.IsNullOrEmpty(_publicrepositoryLine) ? string.Empty : _publicrepositoryLine)
        .Replace("<&PublicAsyncTask&>", string.IsNullOrEmpty(startchild) ? string.Empty : startchild);
        File.WriteAllText(filesavelocation + "/" + "Delete" + entity + "Request.cs", deleteentityrequest);

    }
}