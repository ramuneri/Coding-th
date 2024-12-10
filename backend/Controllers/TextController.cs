using Microsoft.AspNetCore.Mvc;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TextController : ControllerBase
    {
        private readonly VectorService _vectorService;
        private readonly TextService _textService;

        public TextController(VectorService vectorService, TextService textService)
        {
            _vectorService = vectorService;
            _textService = textService;
        }

        [HttpPost("encode")]
        public IActionResult EncodeText([FromBody] TextRequest request)
        {
            try
            {
                double pe = request.Pe;
                int n = request.N;
                int k = request.K;
                string text = request.Text;
                List<List<int>>? gMatrix = request.gMatrix;

                gMatrix = gMatrix ?? _vectorService.GenerateMatrixG(n, k);

                var textChunks = _textService.ConvertTextToBinaryChunks(text, k);
                var encodedChunks = _textService.GetEncodedChunks(n, k, gMatrix, textChunks);
                var receivedChunks = _textService.GetReceivedChunks(n, pe, encodedChunks);
                var primaryReceivedChunks = _textService.GetPrimaryChunks(k, receivedChunks);
                string receivedText = _textService.ConvertChunksToText(primaryReceivedChunks);

                return Ok(new
                {
                    GMatrix = gMatrix,
                    ReceivedText = receivedText,
                    ReceivedChunks = receivedChunks,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("decode")]
        public IActionResult Decode([FromBody] DecodeRequestText request)
        {
            try
            {

                string text = request.Text;
                string receivedText = request.ReceivedText;
                List<List<int>> gMatrix = request.gMatrix;
                List<List<int>> receivedChunks = request.ReceivedChunks;
    
                if (receivedText == null)
                {
                    return BadRequest("Did not get text for decoding.");
                }
                if (gMatrix == null)
                {
                    return BadRequest("Did not get matrix G.");
                }

                int n = gMatrix[0].Count;
                int k = gMatrix.Count;
                List<List<int>> hMatrix = _vectorService.GenerateMatrixH(gMatrix);

                var decodedChunks = _textService.GetDecodedChunks(gMatrix, hMatrix, receivedChunks);
                var primaryDecodedChunks = _textService.GetPrimaryChunks(k, decodedChunks);
                string decodedText = _textService.ConvertChunksToText(primaryDecodedChunks);

                return Ok(new
                {
                    DecodedText = decodedText,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Internal server error");
            }
        }
    }

    public class TextRequest
    {
        required public double Pe { get; set; }
        required public int N { get; set; }
        required public int K { get; set; }
        required public string Text { get; set; }
        required public List<List<int>>? gMatrix { get; set; }
    }

    public class DecodeRequestText
    {
        required public string Text { get; set; }
        required public string ReceivedText { get; set; }
        required public List<List<int>> gMatrix { get; set; }
        required public List<List<int>> ReceivedChunks { get; set; }
    }
}
