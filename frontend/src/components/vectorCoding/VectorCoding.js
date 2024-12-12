import React, { useState } from 'react';
import axios from 'axios';
import './VectorCoding.css';

const VectorCoding = () => {
    const [pe, setPe] = useState('0.1');
    const [n, setN] = useState('7');
    const [k, setK] = useState('4');
    const [m, setM] = useState('1111');
    const [useAutoG, setUseAutoG] = useState(true);
    const [customGMatrix, setCustomGMatrix] = useState('');
    const [gMatrix, setGMatrix] = useState(null);
    const [primaryVector, setPrimaryVector] = useState(null);
    const [encodedVector, setEncodedVector] = useState(null);
    const [receivedVector, setReceivedVector] = useState(null);
    const [decodedVector, setDecodedVector] = useState(null);
    const [errorCount, setErrorCount] = useState(null);
    const [errorPositions, setErrorPositions] = useState([]);
    const [successMessage, setSuccessMessage] = useState('');

    
    const handleEncode = async () => {
        // Checks parameters entered by user and if they match
        if (m.length !== parseInt(k)) {
            alert(`Primary vector must be of length ${k}!`);
            return;
        }
        if (!/^[01]+$/.test(m)) {
            alert('Primary vector must be binary!');
            return;
        }

        // If user entered custom G, it parses it
        let gMatrixToSend = useAutoG ? null : parseCustomGMatrix(customGMatrix);

        // If there is comma in pe field, it is replaced to dot
        const peValue = parseFloat(pe.replace(',', '.'));

        // Checks if custom G is correct
        if (!useAutoG) {
            if (!gMatrixToSend || gMatrixToSend.length !== parseInt(k)) {
                alert(`Custom G matrix must have ${k} rows!`);
                return;
            }
            for (const row of gMatrixToSend) {
                if (row.length !== parseInt(n)) {
                    alert(`Each row in the custom G matrix must have ${n} columns!`);
                    return;
                }
            }
        }

        // Input data to send to backend for encoding
        const vectorData = {
            pe: peValue,
            n: parseInt(n),
            k: parseInt(k),
            vector: m.split('').map(Number),
            gMatrix: gMatrixToSend,
        };

        // Sending data and setting received values
        try {
            setPrimaryVector(null);
            setDecodedVector(null);
            setSuccessMessage('');

            const response = await axios.post('http://localhost:5000/api/vector/encode', vectorData, {
                headers: { 'Content-Type': 'application/json' },
            });
            const { gMatrix, encodedVector, receivedVector, errorCount, errorPositions } = response.data;

            setGMatrix(gMatrix);
            setEncodedVector(encodedVector);
            setReceivedVector(receivedVector.join(''));
            setErrorCount(errorCount !== null ? errorCount : 0);
            setErrorPositions(errorCount > 0 ? errorPositions : '-');
        } catch (error) {
            console.error('Error encoding vector:', error);
        }
    };

    const handleDecode = async () => {
        // Encoding must be done before decoding
        if (!receivedVector) {
            alert('Please encode the vector first.');
            return;
        }

        // Data to send to backend for decoding
        const receivedVectorData = {
            encodedVector: encodedVector,
            receivedVector: receivedVector.split('').map(Number),
            originalVector: m.split('').map(Number),
            gMatrix: gMatrix,
        };


        // Sending data and setting received values
        try {
            const response = await axios.post('http://localhost:5000/api/vector/decode', receivedVectorData);
            const { errorCount, errorPositions, decodedVector, primaryVector, successMessage } = response.data;

            setErrorCount(errorCount !== null ? errorCount : 0);
            setErrorPositions(errorCount > 0 ? errorPositions : '-');
            setDecodedVector(decodedVector);
            setPrimaryVector(primaryVector);
            setSuccessMessage(successMessage);
        } catch (error) {
            console.error('Decoding failed:', error);
        }
    };

    // Called if user modifies received vector
    const handleReceivedVectorChange = (e) => {
        const updatedReceivedVector = e.target.value.replace(/[^01]/g, ''); // Keep only 0s and 1s
        setReceivedVector(updatedReceivedVector); // Save as a string
    
        // Recalculate errors only if the lengths match
        if (encodedVector && updatedReceivedVector && updatedReceivedVector.length === encodedVector.length) {
            const errorData = calculateErrors(
                encodedVector,
                updatedReceivedVector.split('').map(Number) // Convert to a list for calculation
            );
            setErrorCount(errorData.errorCount);
            setErrorPositions(errorData.errorPositions);
        } else {
            // Reset error count and positions if lengths mismatch
            setErrorCount('-');
            setErrorPositions('-');
        }
    
        // Reset decoding-related states
        setDecodedVector(null);
        setPrimaryVector(null);
        setSuccessMessage('');
    
        console.log('Encoded Vector:', encodedVector);
        console.log('Updated Received Vector (as string):', updatedReceivedVector);
    };
    
    
    

    const calculateErrors = (encoded, received) => {
        console.log('In calculateErrors:\n');
        console.log('encoded = ', encoded, ', received = ', received);


        if (encoded.length !== received.length) {
            return { errorCount: null, errorPositions: [] };
        }
    
        let errorCount = 0;
        const errorPositions = [];
    
        for (let i = 0; i < encoded.length; i++) {
            if (encoded[i] !== received[i]) {
                errorCount++;
                errorPositions.push(i);
            }
        }
    
        return { errorCount, errorPositions };
    };
    
    

    // This is called if user wants auto-generated G matrix
    const parseCustomGMatrix = (matrixString) => {
        const parsed = matrixString.split('\n').map((row) => row.split(',').map(Number));
        return parsed;
    };

    const formatMatrix = (matrix) => {
        return matrix ? matrix.map((row) => row.join(',')).join('\n') : '';
    };

    return (
        <div className="container">
            <h2>Vector Encoder/Decoder</h2>

            {/* Fields for user to enter parameters */}
            <form onSubmit={(e) => e.preventDefault()}>
                <div className="group">
                    <label htmlFor="pe">Error probability (p<sub>e</sub>):</label>
                    <input type="number" id="pe" value={pe} onChange={(e) => setPe(e.target.value)} step="0.0001" min="0" max="1" required />
                </div>
                <div className="group">
                    <label htmlFor="n">Code length (n):</label>
                    <input type="number" id="n" value={n} onChange={(e) => setN(e.target.value)} required />
                </div>
                <div className="group">
                    <label htmlFor="k">Code dimension (k):</label>
                    <input type="number" id="k" value={k} onChange={(e) => setK(e.target.value)} required />
                </div>
                <div className="group">
                    <label htmlFor="m">Primary vector (m):</label>
                    <input type="text" id="m" value={m} onChange={(e) => setM(e.target.value)} pattern="[01]+" required />
                </div>

                {/* User chooses matrix G input */}
                <div className="radio-group">
                    <label>
                        <input type="radio" checked={useAutoG} onChange={() => setUseAutoG(true)} />
                        Auto-generate G matrix
                    </label>
                    <label>
                        <input type="radio" checked={!useAutoG} onChange={() => setUseAutoG(false)} />
                        Provide custom G matrix
                    </label>
                </div>

                {/* If custum matrix G, user can enter it here */}
                {!useAutoG && (
                    <div className="group">
                        <label htmlFor="gMatrix">Custom G matrix:</label>
                        <textarea
                            id="gMatrix"
                            value={customGMatrix}
                            onChange={(e) => setCustomGMatrix(e.target.value)}
                            placeholder={`Format example:\n1,0,0\n0,1,1`}
                            rows="5"
                        />
                    </div>
                )}
                <div className="buttons-container">
                    <button type="button" onClick={handleEncode}>Send</button>
                    <button type="button" onClick={handleDecode}>Decode</button>
                </div>
            </form>

            {/* Results after encoding and decoding */}
            <form>

                {/* Shows auto-generate G matrix if user selects it */}
                {useAutoG && gMatrix && (
                    <div className="group">
                        <label>Auto-generated G matrix</label>
                        <textarea id = "gMatrix" value={formatMatrix(gMatrix)} rows={k} readOnly/>
                    </div>
                )}

                {/* Other calculations */}
                <div className="group">
                    <label>Encoded vector (c):</label>
                    <input type="text" value={encodedVector?.join('') || ''} readOnly />
                </div>
                <div className="group">
                    <label>Received vector (r) - editable:</label>
                    <input type="text" value={receivedVector || ''} onChange={handleReceivedVectorChange} />
                </div>
                <div className="group">
                    <label>Number of errors:</label>
                    <input type="text" value={errorCount !== null ? errorCount : '-'} readOnly />
                </div>
                <div className="group">
                    <label>Error positions:</label>
                    <input type="text" value={errorPositions !== '-' ? errorPositions.map((pos) => pos + 1).join(', ') : '-'} readOnly />
                </div>
                <div className="group">
                    <label>Decoded vector (c'):</label>
                    <input type="text" value={decodedVector?.join('') || ''} readOnly />
                </div>
                <div className="group">
                    <label>Primary vector (m'):</label>
                    <input type="text" value={primaryVector?.join('') || ''} readOnly />
                </div>
                
                {/* Informs on decoding success */}
                {successMessage && (
                    <div className="success-message">
                        <label>{successMessage}</label>
                    </div>
                )}
            </form>
        </div>
    );
};

export default VectorCoding;
