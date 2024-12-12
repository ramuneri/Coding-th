using System.Text;

namespace backend.Services
{
    public class TextService
    {
        private readonly VectorService _vectorService; // _vectorService - private field of type VectorService.

        public TextService(VectorService vectorService) // Constructor for the TextService class
        {
            _vectorService = vectorService;
        }

        /** Encodes vectors
        @param cade parameters - n and k, generating matrix, vectors to encode
        @returns encoded vector*/
        public List<List<int>> GetEncodedChunks (int n, int k, List<List<int>> gMatrix, List<List<int>> binaryChunks)
        {
            List<List<int>> encodedChunks = new List<List<int>>();
            foreach (var chunk in binaryChunks)
            {
                List<int> encodedChunk = _vectorService.EncodeVector(n, k, chunk, gMatrix);
                encodedChunks.Add(encodedChunk);              
            }

            return encodedChunks;
        }

        /** Decodes vectors
        @param generating matrix, control matrix, vectors to decode
        @returns if success, vectors without errors */
        public List<List<int>> GetDecodedChunks (List<List<int>> gMatrix, List<List<int>> hMatrix, List<List<int>> receivedChunks)
        {
            int n = gMatrix[0].Count;
            int k = gMatrix.Count;

            List<List<int>> decodedChunks = new List<List<int>>();
            List<(List<int> syndrome, int w)> reducedTable = _vectorService.GenerateReducedStandardTable(n, k, hMatrix);
            
            foreach (var chunk in receivedChunks)
            {
                List<int> decodedChunk = DecodeVectorChunks(chunk, gMatrix, hMatrix, reducedTable);
                decodedChunks.Add(decodedChunk);
            }

            return decodedChunks;
        }

        /** Decodes vector
        @param vector to decode, generating matrix, control matrix, reduced standard table
        @returns if success, vector without errors */
        public List<int> DecodeVectorChunks(List<int> receivedVector, List<List<int>> gMatrix, List<List<int>> hMatrix, List<(List<int>, int)> reducedTable)
        {
            int n = gMatrix[0].Count;
            int k = gMatrix.Count;

            // Calculate the syndrome of the received vector
            List<int> syndrome = _vectorService.CalculateSyndrome(receivedVector, hMatrix);

            // Weight that is asociated with received vectors syndrome
            int w = _vectorService.FindWeightFromSindrome(reducedTable, syndrome);

            if (w == 0) 
            {
                return receivedVector;
            }
            else {
                int wNew = n;
                for (int i = 0; i < n; i++) {
                    receivedVector[i] = receivedVector[i] == 1 ? 0 : 1;
                    syndrome = _vectorService.CalculateSyndrome(receivedVector, hMatrix);
                    wNew = _vectorService.FindWeightFromSindrome(reducedTable, syndrome);

                    if (wNew == 0) {
                        return receivedVector;
                    }
                    else if (wNew < w) {
                        w = wNew;
                    }
                    else {
                        receivedVector[i] = receivedVector[i] == 1 ? 0 : 1;
                    }
                }
            }

            return receivedVector;
        }

        /** Converts text into binary form and splits the binary data into chunks of size k
        @param text to convert, size of binary chunks to split
        @returns binary chunks representing text */
        public (List<List<int>>, List<int> remainingBits) ConvertTextToBinaryChunks(string text, int k)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(text); // Each character to bytes
            List<int> binaryData = new List<int>();

            foreach (var b in textBytes) // Converts each byte to binary
            {
                binaryData.AddRange(ConvertByteToBinary(b));
            }

            List<List<int>> binaryChunks = new List<List<int>>(); // Chunk the binary data
            List<int> remainingBits = new List<int>(); // For remaining bits if left

            for (int i = 0; i < binaryData.Count; i += k)
            {
                List<int> chunk = binaryData.Skip(i).Take(k).ToList();
                if (chunk.Count < k)
                {
                    remainingBits = chunk;
                    break;
                }

                binaryChunks.Add(chunk);
            }



            if (remainingBits.Count > 0)
            {
                Console.WriteLine($"RemainingBits count = {remainingBits.Count}");
                Console.WriteLine(string.Join("", remainingBits)); // Joins the remaining bits for display
            }
            else
            {
                Console.WriteLine($"No remaining bits. RemainingBits count = {remainingBits.Count}");
            }


            return (binaryChunks, remainingBits);
        }

        /** Sends binary chunks (vectors) via tunnel and makes random mistakes
        @param size of vector, error probability, binary chunks to send
        @returns binary chunks after sending via tunnel */
        public List<List<int>> GetReceivedChunks(int n, double pe, List<List<int>> encodedChunks)
        {
            List<List<int>> receivedChunks = new List<List<int>>();
            Random random = new Random();

            foreach (var chunk in encodedChunks)
            {
                List<int> receivedChunk =  SendVectorChunks(n, pe, chunk, random);
                receivedChunks.Add(receivedChunk);
            }

            return receivedChunks;
        }

        /** Sends vector via tunnel and makes random mistakes
        @param encoded vector length, error probability, vector that will be send
        @returns vectors with (probably) mistakes */
        // Just like SendVector in VectorsService but does not initialize random numbers generatos for every vector separately
        public List<int> SendVectorChunks(int n, double pe, List<int> encodedVector, Random random) 
        {
            List<int> receivedVector = new List<int>(new int[n]);

            for (int i = 0; i < n; i++)
            {
                double randomValue = random.NextDouble();
                receivedVector[i] = (randomValue <= pe) ? (encodedVector[i] == 1 ? 0 : 1) : encodedVector[i];
            }

            return receivedVector;
        }

        /** Gets primary chunks (binary vectors)
        @param length of primary vector, chunks (vectors)
        @returns primary vectors */
        public List<List<int>> GetPrimaryChunks (int k, List<List<int>> chunks, List<int> remainingChunks)
        {
            List<List<int>> primaryChunks = new List<List<int>>();
            foreach (var chunk in chunks)
            {
                List<int> primaryChunk = _vectorService.GetPrimaryVector(k, chunk);
                primaryChunks.Add(primaryChunk);
            }
            if (remainingChunks.Count > 0)
            {
                primaryChunks.Add(remainingChunks);
            }

            return primaryChunks;
        }
        
        /** Converts binary data back to text
        @param binary chunks
        @returns text */
        public string ConvertChunksToText(List<List<int>> chunks)
        {
            List<int> binaryData = chunks.SelectMany(chunk => chunk).ToList(); // Combines all binary chunks into one big binary list
            List<byte> textBytes = new List<byte>(); // To store bytes

            for (int i = 0; i < binaryData.Count; i += 8) // Binary to bytes
            {
                if (i + 8 > binaryData.Count) break;

                List<int> binaryChunk = binaryData.Skip(i).Take(8).ToList();
                byte b = ConvertBinaryToByte(binaryChunk);
                textBytes.Add(b);
            }
            string decodedText = Encoding.UTF8.GetString(textBytes.ToArray()); // Bytes to text

            return decodedText;
        }

        /** Convers byte to its binary value
        @param byte
        @returns its binary representation */
        public List<int> ConvertByteToBinary(byte value)
        {
            var binary = new List<int>();
            for (int i = 7; i >= 0; i--)
            {
                binary.Add((value >> i) & 1);
            }

            return binary;
        }

        /** Convers binary value to byte 
        @param binary value
        @returns byte */
        public byte ConvertBinaryToByte(List<int> binary)
        {
            if (binary.Count != 8)
            {
                throw new ArgumentException("Binary list must contain exactly 8 bits.");
            }

            byte value = 0;
            for (int i = 0; i < 8; i++)
            {
                value |= (byte)(binary[i] << (7 - i)); // left shift and do OR with previous val
            }
            
            return value;
        }
    }
}
