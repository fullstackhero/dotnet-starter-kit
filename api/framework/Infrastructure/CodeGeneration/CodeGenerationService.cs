using CodegenCS;

namespace FSH.Framework.Infrastructure.CodeGeneration;

public class CodeGenerationService : ICodeGenerationService
{
    private readonly string _outputDirectory;

    public CodeGenerationService(string outputDirectory)
    {
        _outputDirectory = outputDirectory;
        Directory.CreateDirectory(_outputDirectory);
    }

    public void GenerateCode(ICodeTemplate template, string outputFileName)
    {
        var writer = new CodegenTextWriter();
        template.Generate(writer);
        SaveToFile(writer.ToString(), outputFileName);
    }

    private void SaveToFile(string content, string fileName)
    {
        var filePath = Path.Combine(_outputDirectory, fileName);
        File.WriteAllText(filePath, content);
    }
}
