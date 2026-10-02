import { useState } from 'react';
import { setUserContext } from './api/userContextApi';

import './App.css';


function App() {
    const [currentUser, setCurrentUser] = useState(null);
    const [selectedFile, setSelectedFile] = useState(null);
    const [token, setToken] = useState(null);

    const onFileChange = (event) => {
        setSelectedFile(event.target.files[0]);
    };

    const setUser = async(user) => {
        const { token } = await setUserContext(user);
        setCurrentUser(user);
        setToken(token);
    };

    const switchUser = async () => {
        const nextUser = currentUser === 'Andy' ? 'Bob' : 'Andy';
        setUser(nextUser);
    };

    return (
        <>
            <h1>Mock Module</h1>
            {currentUser === null ? (
                <div>
                    <p>Select a user:</p>
                    <button onClick={() => setUser('Andy')}>Andy</button>
                    <button onClick={() => setUser('Bob')}>Bob</button>

                </div>
            ) : (
                <div>
                        <p>Is logged in: {currentUser}.</p>
                        <br />

                        <button onClick={() => switchUser()}>
                            Switch User
                        </button>
                        <br />

                        <p>Token = {token}</p>

                        <input type="file" onChange={onFileChange} />
                        <br />

                        {selectedFile ? (
                            <div>
                                <h2>File Details:</h2>
                                <p>Name: {selectedFile.name}</p>
                                <p>Type: {selectedFile.type}</p>
                                <p>Size: {selectedFile.size}bytes</p>
                                <button onClick={null /*TODO: Upload to db*/} >Upload!</button>
                            </div>
                        ) : (
                            <p>No file selected.</p>
                        )}
                        <br />

                        <div>
                            <h2>{currentUser}'s Files:</h2>
                        </div>
                </div>
            )}
        </>
    );
};

export default App;