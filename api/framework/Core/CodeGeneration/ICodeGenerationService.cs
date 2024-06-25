namespace FSH.Framework.Infrastructure.CodeGeneration;

public interface ICodeGenerationService
{
    void GenerateCode(ICodeTemplate template, string outputFileName);
}

