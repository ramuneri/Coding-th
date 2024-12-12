using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace backend.Services
{
    public class ImageService
    {
        private readonly VectorService _vectorService;
        private readonly TextService _textService;

        public ImageService(VectorService vectorService, TextService textService)
        {
            _vectorService = vectorService;
            _textService = textService;
        }


        /** Convers image to its binary form and splits into chunks
        @param path to file, size of chunks
        @returns binary chunks */
        public (List<List<int>>, List<int> remainingBits) ConvertImageToBinaryChunks(string filePath, int k)
        {
            // Saves all binary
            List<int> binaryData = new List<int>();

            // To load the image into memory
            // Each pixel treated as having 3 channels (Red, Green, and Blue), each 8 bits
            using (Image<Rgb24> image = Image.Load<Rgb24>(filePath)){
                image.ProcessPixelRows(accessor => // Goes throw image rows
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        // Access all pixels in a row
                        // Extract the R, G, and B values for each pixel
                        Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
                        foreach (var pixel in pixelRow)
                        {
                            binaryData.AddRange(_textService.ConvertByteToBinary(pixel.R));
                            binaryData.AddRange(_textService.ConvertByteToBinary(pixel.G));
                            binaryData.AddRange(_textService.ConvertByteToBinary(pixel.B));
                        }
                    }
                });
            }

            // Divide into chunks
            var binaryChunks = new List<List<int>>();
            var remainingBits = new List<int>();

            for (int i = 0; i < binaryData.Count; i += k)
            {
                List<int> chunk = binaryData.Skip(i).Take(k).ToList();
                if (chunk.Count < k) {
                    remainingBits = chunk;
                    break;
                }
                binaryChunks.Add(chunk);
            }

            return (binaryChunks, remainingBits);
        }


        /** Tries decoding binary image chunks that came from tunnel
        @param chunks from tunnel, generator matrix, control matrix
        @returns decoded image binary chunks */
        public List<List<int>> DecodeImageChunks(List<List<int>> receivedChunks, List<List<int>> gMatrix, List<List<int>> hMatrix)
        {
            if (gMatrix == null || hMatrix == null)
            {
                throw new ArgumentException("gMatrix or hMatrix is null.");
            }

            int n = gMatrix[0].Count;
            int k = gMatrix.Count;

            var decodedChunks = new List<List<int>>();
            List<(List<int> syndrome, int w)> reducedTable = _vectorService.GenerateReducedStandardTable(n, k, hMatrix);

            foreach (var chunk in receivedChunks)
            {
                try
                {
                    var decodedChunk = _textService.DecodeVectorChunks(chunk, gMatrix, hMatrix, reducedTable);
                    decodedChunks.Add(decodedChunk);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception: {ex.Message}");
                }
            }

            return decodedChunks;
        }


        /** Generates image from binary chunks
        @param binary chunks, image parameters
        @returns image */
        public Image<Rgb24> ConvertChunksToImage(List<List<int>> chunks, int width, int height)
        {
            // New blank image
            var image = new Image<Rgb24>(width, height);

            // All chunks into a single list of binary values
            var binaryData = chunks.SelectMany(chunk => chunk).ToList();

            // Convert binary data into pixel values
            List<byte> pixelValues = new List<byte>();
            for (int i = 0; i < binaryData.Count; i += 8)
            {
                // Every 8 bits into a byte
                var binaryByte = binaryData.Skip(i).Take(8).ToList();
                byte value = _textService.ConvertBinaryToByte(binaryByte);
                pixelValues.Add(value);
            }

            // Populate the image with pixel data
            int pixelIndex = 0;
            image.ProcessPixelRows(accessor =>
            {
                for (int y = 0; y < accessor.Height; y++)
                {
                    Span<Rgb24> pixelRow = accessor.GetRowSpan(y);
                    for (int x = 0; x < accessor.Width; x++)
                    {
                        // Extract RGB values from the pixel values list
                        byte r = pixelValues[pixelIndex++];
                        byte g = pixelValues[pixelIndex++];
                        byte b = pixelValues[pixelIndex++];

                        // Set the pixel color
                        pixelRow[x] = new Rgb24(r, g, b);
                    }
                }
            });

            return image;
        }
    }
}
