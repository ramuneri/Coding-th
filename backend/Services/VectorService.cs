namespace backend.Services
{
    public class VectorService
    {

        /** Encodes vector: EncodedVector = vector x gMatrix
        @param cade parameters - n and k, vector to encode, generating matrix
        @returns encoded vector*/
        public List<int> EncodeVector(int n, int k, List<int> vector, List<List<int>> gMatrix)
        {
            List<int> encodedVector = new List<int>(new int[n]);

            for (int j = 0; j < n; j++)
            {
                int sum = 0;
                for (int i = 0; i < k; i++)
                {
                    sum += vector[i] * gMatrix[i][j];
                }
                encodedVector[j] = sum % 2;
            }

            return encodedVector;
        }


        /** Sends vector via tunnel and makes random mistakes
        @param encoded vector length, error probability, vector that will be send
        @returns vectors with (probably) mistakes */
        public List<int> SendVector(int n, double pe, List<int> encodedVector) 
        {
            List<int> receivedVector = new List<int>(new int[n]); // makes all 0
            Random random = new Random();

            for (int i = 0; i < n; i++)
            {
                double randomValue = random.NextDouble();
                receivedVector[i] = (randomValue <= pe) ? (encodedVector[i] == 1 ? 0 : 1) : encodedVector[i];
            }

            return receivedVector;
        }


        /** Decodes vector
        @param vector to decode, generating matrix, control matrix
        @returns if success, encoded vector without errors */
        public List<int> DecodeVector(List<int> receivedVector, List<List<int>> gMatrix, List<List<int>> hMatrix)
        {
            int n = gMatrix[0].Count; // Number of columns in G
            int k = gMatrix.Count; // Number of rows in G

            // Table from coset leaders syndromes and their weight
            List<(List<int> syndrome, int w)> reducedTable = GenerateReducedStandardTable(n, k, hMatrix);
            
            // Calculate the syndrome of the received vector
            List<int> syndrome = CalculateSyndrome(receivedVector, hMatrix);

            // Weight that is asociated with received vectors syndrome
            int w = FindWeightFromSindrome(reducedTable, syndrome);

            if (w == 0)
            {
                return receivedVector;
            }
            else {
                int wNew; // Max weight by default
                for (int i = 0; i < n; i++) {
                    receivedVector[i] = receivedVector[i] == 1 ? 0 : 1;

                    syndrome = CalculateSyndrome(receivedVector, hMatrix);
                    wNew = FindWeightFromSindrome(reducedTable, syndrome);

                    if (wNew == 0) {
                        return receivedVector;
                    } // keep changes, change minimal weight
                    else if (wNew < w) {
                        w = wNew;
                    }
                    else { // revert changes
                        receivedVector[i] = receivedVector[i] == 1 ? 0 : 1;
                    }
                }
            }

            return receivedVector;
        }


        /** Finds weight of associated with specific syndrome from reduced standard table
        @param vector to decode, generating matrix, control matrix
        @returns if success, encoded vector without errors */
        public int FindWeightFromSindrome(List<(List<int> syndrome, int w)> reducedTable, List<int> syndrome)
        {
            int w = -1; // Default

            foreach (var entry in reducedTable)
            {
                if (entry.syndrome.SequenceEqual(syndrome))
                {
                    w = entry.w;
                    break;
                }
            }

            return w;
        }


        /** Extracts primary vector = mG = m(I|A) = (mI|mA) = (m|mA) and I is of size k x k
        @param dimension of generating matrix, vector of size n
        @returns vector of size k */
        public List<int> GetPrimaryVector(int k, List<int> vector)
        {
            return vector.Take(k).ToList();
        }
        

        /** Creates a reduced standard table from coset leaders syndromes and their weight
        @param code parameters, control matrix
        @returns reduced standard table */
        public List<(List<int> syndrome, int)> GenerateReducedStandardTable(int n, int k, List<List<int>> hMatrix)
        {
            List<(List<int> syndrome, int vectorWeight)> reducedTable = new List<(List<int>, int)>();

            // All binary vectors of length n, sorted from smallest weight
            List<List<int>> allVectors = GenerateAllBinaryVectors(n);
            allVectors.Sort((a, b) => CalculateWeight(a).CompareTo(CalculateWeight(b)));

            // For tracking used syndromes; string - for easier comparison
            HashSet<string> usedSyndromes = new HashSet<string>();

            foreach (var vector in allVectors)
            {
                List<int> syndrome = CalculateSyndrome(vector, hMatrix);
                string syndromeStr = string.Join("", syndrome);

                if (!usedSyndromes.Contains(syndromeStr))
                {
                    reducedTable.Add((syndrome, CalculateWeight(vector)));
                    usedSyndromes.Add(syndromeStr); // Mark syndrome as used
                }

                // Stop when enought coset leaders 2^(n-k)
                if (reducedTable.Count == (1 << (n - k)))
                {
                    break;
                }
            }

            return reducedTable;
        }


        /** Generates all binary vectors
        @param length of binary vectors
        @returns 2^n binary vectors */
        public static List<List<int>> GenerateAllBinaryVectors(int n)
        {
            int numOfVectors = 1 << n; // Number of vectors in standart table is 2^n
            List<List<int>> vectors = new List<List<int>>();

            for (int i = 0; i < numOfVectors; i++)
            {
                List<int> vector = new List<int>();
                for (int j = n - 1; j >= 0; j--)
                { // Moves all bits of i to the right by j and ANDs 1
                    vector.Add((i >> j) & 1);
                }
                vectors.Add(vector);
            }

            return vectors;
        }


        /** Generates generating matrix in a standard form - G(I|A) where I = k x k and A = k x (n - k)
        @param code parameters - matrix height and length
        @returns Generating matrix */
        public List<List<int>> GenerateMatrixG(int n, int k)
        {
            List<List<int>> g = new List<List<int>>();
            Random rand = new Random();

            for (int i = 0; i < k; i++) // goes through every rows
            {
                g.Add(new List<int>(new int[n]));  // adds it to g with all 0 elements
                g[i][i] = 1;
            }

            for (int i = 0; i < k; i++)
            {
                for (int j = k; j < n; j++)
                {
                    g[i][j] = rand.Next(0, 2);
                }
            }

            return g;
        }


        /** Generates control matrix where H(-A^T|I) where A = k x (n - k) transposed and I is (n - k) x (n - k)
        @param generating matrix
        @returns Control matrix */
        public List<List<int>> GenerateMatrixH(List<List<int>> g)
        {
            int n = g[0].Count;
            int k = g.Count;

            List<List<int>> h = new List<List<int>>();
            List<List<int>> a = new List<List<int>>();

            // Extracts columns from k to n (A matrix)
            for (int i = 0; i < k; i++)
            {
                a.Add(g[i].Skip(k).ToList());
            }

            for (int i = 0; i < (n - k); i++) 
            {
                List<int> hRow = new List<int>();

                // Transposes matrix A
                for (int j = 0; j < k; j++)
                {
                    hRow.Add(a[j][i]);
                }

                // Remaining part of H is identity matrix I of size n - k
                for (int j = 0; j < (n - k); j++)
                {
                    hRow.Add(i == j ? 1 : 0);
                }

                h.Add(hRow);
            }

            return h;
        }


        /** Calculates syndrome
        @param vectors which syndrom to calculate, control matrix
        @returns syndrome */
        public List<int> CalculateSyndrome(List<int> vector, List<List<int>> h)
        {
            List<int> syndrome = new List<int>(new int[h.Count]); // Syndromes lenght is n - k (number of H rows)

            for (int i = 0; i < h.Count; i++)
            {
                int sum = 0;
                for (int j = 0; j < h[i].Count; j++)
                {
                    sum += vector[j] * h[i][j];
                }
                syndrome[i] = sum % 2;
            }

            return syndrome;
        }


        /** Calculates vector's weight
        @param vector
        @returns vector's weight */
        public int CalculateWeight(List<int> vector)
        {
            return vector.Count(x => x == 1);
        }


        /** Calculates errors after sending vector
        @param vector before and after sending
        @returns number of errors */
        public int CountErrors(List<int> encodedVector, List<int> receivedVector)
        {
            if (encodedVector == null || receivedVector == null || encodedVector.Count != receivedVector.Count)
            {
                return 0;
            }

            int errorCount = 0;
            for (int i = 0; i < encodedVector.Count; i++)
            {
                if (encodedVector[i] != receivedVector[i])
                {
                    errorCount++;
                }
            }

            return errorCount;
        }


        /** Gets errors positions
        @param vector before and after sending
        @returns positions of errors */
        public List<int> GetErrorPositions(List<int> encodedVector, List<int> receivedVector)
        {
            if (encodedVector == null || receivedVector == null || encodedVector.Count != receivedVector.Count)
            {
                return new List<int>();
            }

            List<int> errorPositions = new List<int>();
            for (int i = 0; i < encodedVector.Count; i++)
            {
                if (encodedVector[i] != receivedVector[i])
                {
                    errorPositions.Add(i);
                }
            }

            return errorPositions;
        }

    }
}
