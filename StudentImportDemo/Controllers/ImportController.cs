using DocumentFormat.OpenXml.Office2010.Word;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.AspNetCore.Mvc;
using StudentImportDemo.Services;

namespace StudentImportDemo.Controllers;

[ApiController]
[Route("api/students")]
public class ImportController : Controller
{
    private readonly IImport _import;
    // Validate cho vao middleware
    // lam dynamic cai ham len co the ap dung OCP trong solid, co the tai su dung ham Read
    // Lam sao check neu co loi tren tung Row, tra ve mot cai status group
    // neu ma file lon thi sao 1 trieu row
    // co cach nao chay nhieu buck nho thay vi chay tung row
    // case khach hang import bi time out roi khach hang import lai thi sao
    // lam sao khi luu xuong database thi kiem tra de biet co trong database hay chua    
    // Doc lai N-layer va DDD de sap xep lai cau truc file code
    public ImportController(IImport import)
    {
        _import = import;
    }
    [HttpPost("import")]
    public IActionResult Import([FromForm] IFormFile file)
    {
        var stream = file.OpenReadStream();
        var studentData = _import.Read(stream);
        return Ok(studentData);
    }
}