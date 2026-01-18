let quill;
window.initQuill = (divId) => {
    quill = new Quill(`#${divId}`, { theme: "snow" });
}

window.getQuillHtml = () => { return quill.root.innerHTML; }
window.setQuillHtml = (htmlContent) => { quill.root.innerHTML = htmlContent; }