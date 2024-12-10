import React from 'react';
import { Link } from 'react-router-dom';
import './MainPage.css';

const MainPage = () => {
    return (
        <div className="main-container">
            <h1>Linear Codes</h1>
            <h2>Encoding & Decoding</h2>
            <div className="options-container">
                <Link to="/encode/vector">
                    <button className="button">Vector</button>
                </Link>
                <Link to="/encode/text">
                    <button className="button">Text</button>
                </Link>
                <Link to="/encode/image">
                    <button className="button">Image</button>
                </Link>
            </div>

            {/* User Guide Section */}
            <div className="user-guide">
                <h3>How to Use</h3>
                <ol>
                    <li>
                        <strong>Choose Action:</strong> Select what you want to send/decode.
                    </li>
                    <li>
                        <strong>Input Data:</strong> Enter the required data into the provided fields.
                    </li>
                    <li>
                        <strong>Encoding Process:</strong>
                        <ul>
                        <li>Press the <span className="emphasis">Send</span> button to encode your data.</li>
                        <li>Once encoded, the result will be displayed.</li>
                            <li>If you're encoding a vector:
                                <ul>
                                    <li>The number of errors and their positions will be shown.</li>
                                    <li>You can manually edit the received vector to adjust error positions if needed.</li>
                                </ul>
                            </li>
                        </ul>
                    </li>
                    <li>
                        <strong>Decoding Process:</strong>
                        <ul>
                            <li>Press the <span className="emphasis">Decode</span> button to decode the data.</li>
                            <li>If you made changes to the received vector, the number of errors and their <strong>positions</strong> will update accordingly.</li>
                            <li>You will be notified if decoding was successful.</li>
                        </ul>
                    </li>
                    <li>
                        <strong>In case of decoding failure try lowering:</strong>
                        <ul>
                            <li>Error Probability</li>
                            <li>Code Rate (k/n)</li>
                        </ul>
                    </li>
                </ol>
            </div>

        </div>
    );
};

export default MainPage;
