namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class UpdateEntityRequest
{
    public UpdateEntityRequest(string pathtodata, string filesavelocation, string pathtobasicsources, string stringnamespace, string entity, string entitynameplural, string eventpath, string validatortype, string validatorname)
    {
        string basicsources = pathtobasicsources + "UpDateEntityRequest.txt";
        string thenamespace = stringnamespace + entitynameplural;
        string entitytolower = entity.ToLower();
        string updateentityrequest = string.Empty;
        string request = string.Empty;
        string datap = pathtodata + "/" + entity + ".cs";
        string relationalLines = string.Empty;
        string[] columnLines = File.ReadAllLines(datap);
        string propertyLines = string.Empty;
        string inputLine = string.Empty;
        string getSet = string.Empty;
        foreach (string line in columnLines)
        {
            inputLine = string.Empty;
            getSet = string.Empty;
            string[] parts = line.Split();
            for (int i = 0; i <= parts.Length; i++)
            {
                if (parts.Length > i + 2)
                {
                    // skip the code part in the entity class
                    bool skipme = parts[i + 1].Length >= entity.Length ? parts[i + 1].Substring(0, entity.Length) == entity : false;

                    if (parts[i] == "public" && parts[i + 1] != "virtual" && parts[i + 1] != "class" && !skipme)
                    {
                        inputLine = parts[i] + " " + parts[i + 1] + " " + parts[i + 2];

                        getSet = string.Equals(parts[i + 1], "string") ? "{ get; set; } = default!;" : "{ get; set; }";
                        propertyLines = propertyLines + "\t" + inputLine + " " + getSet;
                        if (i < parts.Length)
                        {
                            propertyLines = propertyLines + Environment.NewLine;
                        }

                        request = request + " request." + parts[i + 2] + ", ";

                    }
                }
            }
        }

        if (request.Length > 0)
        {
            request = request.Trim();
            request = request.Substring(0, request.Length - 1);
        }

        updateentityrequest = File.ReadAllText(basicsources)
            .Replace("<&EntityPlural&>", entitynameplural)
            .Replace("<&EventsPath&>", eventpath)
            .Replace("<&PropertyLines&>", propertyLines)
            .Replace("<&StringNameSpace&>", thenamespace)
            .Replace("<&Entity&>", entity)
            .Replace("<&EntityToLower&>", entitytolower)
            .Replace("<&Request&>", request)
            .Replace("<&ValidatorName&>", validatorname)
            .Replace("<&ValidatorNameToLower&>", validatorname.ToLower());


        File.WriteAllText(filesavelocation + "/" + "Update" + entity + "Request.cs", updateentityrequest);
    }
}
