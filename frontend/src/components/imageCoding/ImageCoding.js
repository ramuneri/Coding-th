import React, { useState } from 'react';
import axios from 'axios';
import'./ImageCoding.css'

const ImageCoding = () => {
    const [pe, setPe] = useState('0.05');
    const [n, setN] = useState('12');
    const [k, setK] = useState('4');
    const [imagePreview, setImagePreview] = useState(null);
    const [useAutoG, setUseAutoG] = useState(true);
    const [customGMatrix, setCustomGMatrix] = useState('');
    const [primaryImage, setPrimaryImage] = useState(null);
    const [receivedImage, setReceivedImage] = useState(null);
    const [receivedChunks, setReceivedChunks] = useState(null);
    const [decodedImage, setDecodedImage] = useState(null);
    const [gMatrix, setGMatrix] = useState(null);

    const [width, setWidth] = useState(null);
    const [height, setHeight] = useState(null);

    const handleEncode = async () => {
        if (!primaryImage || !primaryImage.type.includes('bmp')) {
            alert('Please upload a valid BMP image.');
            return;
        }        

        let gMatrixToSend = useAutoG ? null : parseCustomGMatrix(customGMatrix);

        const formData = new FormData();
        formData.append('pe', parseFloat(pe.replace(',', '.')));
        formData.append('n', n);
        formData.append('k', k);
        formData.append('file', primaryImage);
        formData.append('useAutoG', useAutoG);

        if (!useAutoG) {
            formData.append('gMatrix', JSON.stringify(gMatrixToSend));
        }
        
        try {
            setDecodedImage(null);
            const response = await axios.post('http://localhost:5000/api/image/encode', formData, {
                headers: { 'Content-Type': 'multipart/form-data' },
            });
            const { gMatrix, receivedImage, receivedChunks, width, height } = response.data;

            setGMatrix(gMatrix);
            setReceivedImage(`data:image/bmp;base64,${receivedImage}`);
            setReceivedChunks(receivedChunks);
            setWidth(width);
            setHeight(height);
        } catch (error) {
            console.error('Error encoding image:', error);
        }
    };

    const handleDecode = async () => {
        if (!receivedImage) {
            alert('Please encode the image first.');
            return;
        }
    
        const decodeImageData = {
            gMatrix: gMatrix,
            receivedChunks: receivedChunks,
            width: width,
            height: height,
        };

        try {
            const response = await axios.post('http://localhost:5000/api/image/decode', decodeImageData, {
                headers: { 'Content-Type': 'application/json' },
            });
            const { decodedImage } = response.data;
        
            setDecodedImage(`data:image/bmp;base64,${decodedImage}`);
        } catch (error) {
            console.error('Error decoding image:', error);
            if (error.response) {
                console.error('Error details:', error.response.data);
            }
        }
    };
    
    const handleFileChange = (e) => { // e - event object that contains information about the input change
        const file = e.target.files[0]; // contains all the files selected by the user
        setPrimaryImage(file);

        const reader = new FileReader(); // FileReader object allows reading the contents of a file
        reader.onload = () => {
            setImagePreview(reader.result);
        };
        if (file) reader.readAsDataURL(file);
    };

    const parseCustomGMatrix = (matrixString) => {
        return matrixString.split('\n').map(row => row.split(',').map(Number));
    };

    return (
        <div className="container">
            <form onSubmit={(e) => e.preventDefault()}>
                <div className="buttons-container">

                    <label htmlFor="file" className="button">
                        Upload BMP Image
                    </label>
                    <input 
                        type="file"
                        id="file"
                        accept=".bmp"
                        onChange={handleFileChange}
                        style={{ display: 'none' }}
                        required
                    />
                </div>

                <div className="group-img">
                    {imagePreview && (
                        <div className="image">
                            <label>Your image:</label>
                            <img
                                src={imagePreview}
                                alt="Uploaded Preview"
                            />
                        </div>
                    )}
                    {receivedImage && (
                        <div className="image">
                            <label>Received Image (with errors):</label>
                            <img
                                src={receivedImage}
                                alt="Uploaded Preview"
                            />
                        </div>
                    )}

                    {decodedImage && (
                        <div className="image">
                            <label>Decoded image:</label>
                            <img
                                src={decodedImage}
                                alt="Uploaded Preview"
                            />
                        </div>
                    )}
                </div>

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
                    <label htmlFor="n">Code Length (n):</label>
                    <input
                        type="number"
                        id="n"
                        value={n}
                        onChange={(e) => setN(e.target.value)}
                        required
                    />
                </div>
                <div className="group">
                    <label htmlFor="k">Code Dimension (k):</label>
                    <input
                        type="number"
                        id="k"
                        value={k}
                        onChange={(e) => setK(e.target.value)}
                        placeholder="Enter code dimension"
                        required
                    />
                </div>

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

                {!useAutoG && (
                    <div className="group">
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

        </div>
    );
};

export default ImageCoding;
