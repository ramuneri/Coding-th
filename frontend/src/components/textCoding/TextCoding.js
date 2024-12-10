import React, { useState } from 'react';
import axios from 'axios';
import './TextCoding.css';

const TextCoding = () => {
    const [pe, setPe] = useState('0.05');
    const [n, setN] = useState('12');
    const [k, setK] = useState('4');
    const [text, setText] = useState('Hello! You can write your text here');
    const [useAutoG, setUseAutoG] = useState(true);
    const [customGMatrix, setCustomGMatrix] = useState('');
    const [gMatrix, setGMatrix] = useState(null);
    const [receivedText, setReceivedText] = useState(null);
    const [decodedText, setDecodedText] = useState(null);
    const [successMessage, setSuccessMessage] = useState(null);
    const [receivedChunks, setReceivedChunks] = useState(null);

    const handleEncode = async () => {
        let gMatrixToSend = useAutoG ? null : parseCustomGMatrix(customGMatrix);
        const peValue = parseFloat(pe.replace(',', '.'));

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

        const textData = {
            pe: peValue,
            n: parseInt(n),
            k: parseInt(k),
            text: text,
            gMatrix: gMatrixToSend,
        };  

        try {
            setDecodedText('');
            setSuccessMessage(null);

            const response = await axios.post('http://localhost:5000/api/text/encode', textData, {
                headers: { 'Content-Type': 'application/json' },
            });
            const { gMatrix, receivedText, receivedChunks } = response.data;

            setGMatrix(gMatrix);
            setReceivedText(receivedText);
            setReceivedChunks(receivedChunks);
        } catch (error) {
            console.error('Error encoding text:', error);
        }
    };

    const handleDecode = async () => {
        if (!receivedText) {
            alert('Please encode the text first.');
            return;
        }

        const receivedTextData = {
            n: parseInt(n),
            k: parseInt(k),
            text: text,
            receivedText: receivedText,
            gMatrix: gMatrix,
            receivedChunks: receivedChunks,
        };

        try {
            const response = await axios.post('http://localhost:5000/api/text/decode', receivedTextData);
            const { decodedText } = response.data;

            setDecodedText(decodedText);
            setSuccessMessage(true);
        } catch (error) {
            setSuccessMessage(false);
            console.error('Decoding failed:', error);
        }
    };

    const parseCustomGMatrix = (matrixString) => {
        const parsed = matrixString.split('\n').map((row) => row.split(',').map(Number));
        return parsed;
    };

    return (
        <div className="container">
            <h2>Text Encoder/Decoder</h2>

            <form onSubmit={(e) => e.preventDefault()}>
                <div className="group">
                    <label htmlFor="pe">Error Probability (p<sub>e</sub>):</label>
                    <input
                        type="number"
                        id="pe"
                        value={pe}
                        onChange={(e) => setPe(e.target.value)}
                        step="0.0001"
                        min="0"
                        max="1"
                        required
                    />
                </div>
                <div className="group">
                    <label htmlFor="n">Code length (n):</label>
                    <input
                        type="number"
                        id="n"
                        value={n}
                        onChange={(e) => setN(e.target.value)}
                        required
                    />
                </div>
                <div className="group">
                    <label htmlFor="k">Code dimension / chunk Size (k):</label>
                    <input
                        type="number"
                        id="k"
                        value={k}
                        onChange={(e) => setK(e.target.value)}
                        required
                    />
                </div>
                <div className="group">
                    <label htmlFor="text">Text:</label>
                    <textarea
                        id="text"
                        value={text}
                        onChange={(e) => setText(e.target.value)}
                        rows = "3"
                        required
                    />
                </div>

                <div className="radio-group">
                    <label>
                        <input
                            type="radio"
                            checked={useAutoG}
                            onChange={() => setUseAutoG(true)}
                        />
                        Auto-generate G matrix
                    </label>
                    <label>
                        <input
                            type="radio"
                            checked={!useAutoG}
                            onChange={() => setUseAutoG(false)}
                        />
                        Provide custom G matrix
                    </label>
                </div>

                {!useAutoG && (
                    <div className="form-group">
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

            <form>
                <div className="group">
                    <label>Received text:</label>
                    <textarea 
                        value={receivedText || ''} 
                        readOnly 
                        rows="3" 
                        style={{ width: '100%' }} 
                    />
                </div>
                <div className="group">
                    <label>Decoded text:</label>
                    <textarea 
                        value={decodedText || ''} 
                        readOnly 
                        rows="3" 
                        style={{ width: '100%' }} 
                    />
                </div>
            </form>

            {successMessage !== null && (
                <form>
                    <div className="success-message">
                        {successMessage ? "Decoding process has finished" : "Decoding has failed"}
                    </div>
                </form>
            )}
        </div>
    );
};

export default TextCoding;
