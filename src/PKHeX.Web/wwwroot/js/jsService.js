window.downloadFileFromStream = async (fileName, contentStreamReference) => {
    const arrayBuffer = await contentStreamReference.arrayBuffer();
    const blob = new Blob([arrayBuffer]);
    const url = URL.createObjectURL(blob);
    const anchorElement = document.createElement('a');
    anchorElement.href = url;
    anchorElement.download = fileName ?? '';
    anchorElement.click();
    anchorElement.remove();
    URL.revokeObjectURL(url);
}

window.encryptEcb = function (key, data) {
    const keyHex = CryptoJS.enc.Hex.parse(key);
    const encrypted = CryptoJS.AES.encrypt(CryptoJS.enc.Hex.parse(data), keyHex, {
        mode: CryptoJS.mode.ECB,
        padding: CryptoJS.pad.NoPadding
    });
    return encrypted.ciphertext.toString(CryptoJS.enc.Hex);
};

window.decryptEcb = function (key, data) {
    var keyHex = CryptoJS.enc.Hex.parse(key);
    var encryptedHexStr = CryptoJS.enc.Hex.parse(data);
    var encryptedBase64Str = CryptoJS.enc.Base64.stringify(encryptedHexStr);
    var decrypted = CryptoJS.AES.decrypt(encryptedBase64Str, keyHex, {
        mode: CryptoJS.mode.ECB,
        padding: CryptoJS.pad.NoPadding
    });
    return decrypted.toString(CryptoJS.enc.Hex);
};

window.md5Hash = function (data) {
    var wordArray = CryptoJS.lib.WordArray.create(data);
    var hash = CryptoJS.MD5(wordArray);
    return hash.words.map(word => [
        (word >> 24) & 0xff,
        (word >> 16) & 0xff,
        (word >> 8) & 0xff,
        word & 0xff
    ]).flat();
}