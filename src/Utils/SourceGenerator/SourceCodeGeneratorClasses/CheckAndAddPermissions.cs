namespace FSH.WebApi.Utils.SourceGenerator.SourceCodeGeneratorClasses;
internal class CheckAndAddPermissions
{
    public CheckAndAddPermissions(string pathtobasicsources, string pathtopermissions, string entityplural)
    {
        if (File.Exists(pathtopermissions))
        {
            string permissions = File.ReadAllText(pathtopermissions);
            bool canaddpermissions = permissions.IndexOf(entityplural) == -1;
            bool codeblockfound = false;
            bool firsttimecontains = true;
            if (canaddpermissions)
            {

                string addresource = File.ReadAllText(pathtobasicsources + "FSHResource.txt")
                     .Replace("<&EntityPlural&>", entityplural);
                string addthispermission = File.ReadAllText(pathtobasicsources + "FSHPermissions.txt")
                    .Replace("<&EntityPlural&>", entityplural);

                string newfile = string.Empty;

                // read FSHPermissions again line per line
                string[] columnLines = File.ReadAllLines(pathtopermissions);

                // where is the last line of => public static class FSHResource
                foreach (string line in columnLines)
                {

                    if (line == "public static class FSHResource")
                    {
                        codeblockfound = true;
                    }

                    if (line == "}" && codeblockfound)
                    {
                        newfile = newfile + addresource + Environment.NewLine;
                        codeblockfound = false;
                    }

                    if (line.Contains("FSHResource.Tenants") && firsttimecontains)
                    {
                        newfile = newfile + addthispermission;
                        firsttimecontains = false;
                    }

                    // split the line in words
                    // string[] parts = line.Split();

                    newfile = newfile + line + Environment.NewLine;

                    // string to be found = "public const string"

                }

                File.WriteAllText(pathtopermissions, newfile);
            }
        }
    }
}
