import { useEffect, useState } from 'react';
import { getFilesList } from '../api/getFilesListApi';
    function DisplayFiles(token) {
        var fileList = getFilesList(token);
}

export default DisplayFiles;