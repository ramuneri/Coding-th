using backend.Services;
using SixLabors.ImageSharp;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp.PixelFormats;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImageController : ControllerBase
    {
        private readonly VectorService _vectorService;
        private readonly TextService _textService;
        private readonly ImageService _imageService;

        public ImageController(VectorService vectorService, TextService textService, ImageService imageService)
        {
            _vectorService = vectorService;
            _textService = textService;
            _imageService = imageService;
        }

        [HttpPost("encode")]
        public async Task<IActionResult> EncodeImage([FromForm] ImageRequest request)
        {
            try
            {
                double pe = request.Pe;
                int n = request.N;
                int k = request.K;
                IFormFile file = request.File;
                List<List<int>>? gMatrix = request.gMatrix;

                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded.");
                }

                gMatrix = gMatrix ?? _vectorService.GenerateMatrixG(n, k);

                // Save uploaded file temporarily
                string tempFilePath = Path.GetTempFileName(); // Stores the path to the temporary file

                // FileStream - points to the file at tempFilePath
                // FileMode.Create  - ensures that file is overwritten/created
                // Reads the uploaded file in chunks and writes it to the file stream
                // Copies the content of the uploaded image file to the stream associated with tempFilePath
                using (var stream = new FileStream(tempFilePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Load and process image
                // Reads the image from tempFilePath
                // Gets dimentions
                int width, height;
                using (Image<Rgb24> image = Image.Load<Rgb24>(tempFilePath)) 
                {
                    width = image.Width;
                    height = image.Height;
                }

                // Encode the image
                var (binaryChunks, remainingBits) = _imageService.ConvertImageToBinaryChunks(tempFilePath, k);
                var encodedChunks = _textService.GetEncodedChunks(n, k, gMatrix, binaryChunks);
                var receivedChunks = _textService.GetReceivedChunks(n, pe, encodedChunks);
                var primaryReceivedChunks = _textService.GetPrimaryChunks(k, receivedChunks, remainingBits);

                // Convert chunks back to image
                Image<Rgb24>? receivedImage = _imageService.ConvertChunksToImage(primaryReceivedChunks, width, height);

                // Convert image to Base64
                string receivedImageBase64;
                using (var ms = new MemoryStream())
                {
                    receivedImage.SaveAsBmp(ms);
                    receivedImageBase64 = Convert.ToBase64String(ms.ToArray());
                }

                return Ok(new
                {
                    GMatrix = gMatrix,
                    ReceivedImage = receivedImageBase64,
                    ReceivedChunks = receivedChunks,
                    RemainingBits = remainingBits,
                    Width = width,
                    Height = height,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("decode")]
        public IActionResult Decode([FromBody] DecodeRequestImage request)
        {
            try
            {
                var gMatrix = request.gMatrix;
                var receivedChunks = request.ReceivedChunks;
                var remainingBits = request.RemainingBits;
                int width = request.Width;
                int height = request.Height;

                if (gMatrix == null)
                {
                    return BadRequest("Did not get matrix G.");
                }            

                int n = gMatrix[0].Count;
                int k = gMatrix.Count;
                var hMatrix = _vectorService.GenerateMatrixH(gMatrix);

                var decodedChunks = _imageService.DecodeImageChunks(receivedChunks, gMatrix, hMatrix);
                var primaryDecodedChunks = _textService.GetPrimaryChunks(k, decodedChunks, remainingBits);
                Image<Rgb24>? decodedImage = _imageService.ConvertChunksToImage(primaryDecodedChunks, width, height);

                // Convert decoded image to Base64
                string decodedImageBase64;
                using (var ms = new MemoryStream())
                {
                    decodedImage.SaveAsBmp(ms);
                    decodedImageBase64 = Convert.ToBase64String(ms.ToArray());
                }

                return Ok(new 
                { 
                    DecodedImage = decodedImageBase64
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, "Internal server error");
            }
        }

        public class ImageRequest
        {
            required public double Pe { get; set; }
            required public int N { get; set; }
            required public int K { get; set; }
            required public IFormFile File { get; set; } = null!;
            required public List<List<int>>? gMatrix { get; set; }
        }

        public class DecodeRequestImage
        {
            required public List<List<int>> gMatrix { get; set; }
            required public List<List<int>> ReceivedChunks { get; set; }
            required public List<int> RemainingBits { get; set; }
            required public int Width { get; set; }
            required public int Height { get; set; }
        }
    }
}
