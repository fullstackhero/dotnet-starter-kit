function DownloadFile(fileName, fileTypeString, fileByteArray) {
    var link = document.createElement('a');
    link.download = fileName;
    link.href = fileTypeString +", " + fileByteArray;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}