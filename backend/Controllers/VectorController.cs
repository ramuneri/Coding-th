using Microsoft.AspNetCore.Mvc;
using backend.Services;

namespace backend.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class VectorController : ControllerBase
    {
        // _vectorService stores the VectorService instance
        private readonly VectorService _vectorService;

        // Constructor for the VectorController class
        public VectorController(VectorService vectorService)
        {
            // Assigns the injected VectorService instance to the private readonly field
            _vectorService = vectorService;
        }

        [HttpPost("encode")]
        public IActionResult Encode([FromBody] VectorRequest request)
        {
            try
            {
                // Extracts individual values from the request object - VectorRequest
                double pe = request.Pe;
                int n = request.N;
                int k = request.K;
                List<int> vector = request.Vector;
                List<List<int>>? gMatrix = request.gMatrix;

                // Checks
                if (vector.Count != k)
                {
                    return BadRequest($"Primary vector length should be exactly {k}");
                }
                if (vector.Any(x => x != 0 && x != 1))
                {
                    return BadRequest("Primary vector must be binary (contain only 0 and 1).");
                }

                // Generates matrix G if needed
                gMatrix ??= _vectorService.GenerateMatrixG(n, k);

                // Encoding and making mistakes
                List<int> encodedVector = _vectorService.EncodeVector(n, k, vector, gMatrix);
                List<int> receivedVector = _vectorService.SendVector(n, pe, encodedVector);

                // Get the number of errors and their positions
                int errorCount = _vectorService.CountErrors(encodedVector, receivedVector);
                List<int> errorPositions = _vectorService.GetErrorPositions(encodedVector, receivedVector);

                // Response statement
                return Ok(new
                {
                    GMatrix = gMatrix,
                    EncodedVector = encodedVector,
                    ReceivedVector = receivedVector,
                    ErrorCount = errorCount,
                    ErrorPositions = errorPositions,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, "Internal server error");
            }
        }


        [HttpPost("decode")]
        public IActionResult Decode([FromBody] DecodeVectorRequest request)
        {
            try
            {
                List<int> encodedVector = request.EncodedVector;
                List<int> receivedVector = request.ReceivedVector;
                List<int> originalVector = request.OriginalVector;
                List<List<int>> gMatrix = request.gMatrix;

                if (receivedVector == null || receivedVector.Count == 0)
                {
                    return BadRequest("Did not get vector for decoding.");
                }
                if (gMatrix == null)
                {
                    return BadRequest("Did not get matrix G.");
                }

                // Generates matrix H
                List<List<int>> hMatrix = _vectorService.GenerateMatrixH(gMatrix);

                // Decodes the received vector
                List<int> decodedVector = _vectorService.DecodeVector(receivedVector, gMatrix, hMatrix);

                // Calculate error count and positions
                int errorCount = _vectorService.CountErrors(encodedVector, receivedVector);
                List<int> errorPositions = _vectorService.GetErrorPositions(encodedVector, receivedVector);

                // Calculate the primary vector based on the decoded vector
                int primaryVectorLength = gMatrix.Count; // primary vector is of size k (number of G rows)
                List<int> primaryVector = _vectorService.GetPrimaryVector(primaryVectorLength, decodedVector);

                // Check if decoding was successful by comparing the original and decoded vectors
                bool isDecodingSuccessful = Enumerable.SequenceEqual(originalVector, primaryVector);
                string successMessage = isDecodingSuccessful ? "Decoding successful!" : "Decoding finished with errors.";

                return Ok(new
                {
                    ErrorCount = errorCount,
                    ErrorPositions = errorPositions,
                    DecodedVector = decodedVector,
                    PrimaryVector = primaryVector,
                    SuccessMessage = successMessage
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Decode: {ex.Message}");
                return StatusCode(500, "An unexpected error occurred during decoding.");
            }
        }


        public class VectorRequest
        {
            required public double Pe { get; set; }
            required public int N { get; set; }
            required public int K { get; set; }
            required public List<int> Vector { get; set; }
            required public List<List<int>>? gMatrix { get; set; }
        }

        public class DecodeVectorRequest
        {
            required public List<int> EncodedVector { get; set; }
            required public List<int> ReceivedVector { get; set; }
            required public List<int> OriginalVector { get; set; }
            required public List<List<int>> gMatrix { get; set; }
        }
    }
}
