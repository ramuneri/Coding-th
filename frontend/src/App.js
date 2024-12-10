import React from 'react';
import { BrowserRouter as Router, Route, Routes } from 'react-router-dom';
import MainPage from './components/mainPage/MainPage';
import VectorCoding from './components/vectorCoding/VectorCoding';
import TextCoding from './components/textCoding/TextCoding';
import ImageCoding from './components/imageCoding/ImageCoding';
import Navbar from './components/navbar/Navbar';
import './App.css';

const App = () => {
    return (
        <Router>
            <Routes>
                <Route path="/" element={<><Navbar /><MainPage /></>} />
                <Route path="/encode/vector" element={<><Navbar /><VectorCoding /></>} />
                <Route path="/encode/text" element={<><Navbar /><TextCoding /></>} />
                <Route path="/encode/image" element={<><Navbar /><ImageCoding /></>} />
            </Routes>
        </Router>
    );
};

export default App;
