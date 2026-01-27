let quill;
window.initQuill = (divId, dotNetObj) => {
    quill = new Quill(`#${divId}`, {
        theme: "snow",
        modules: {
            toolbar: "#journal-toolbar"
        }
    });

    if (dotNetObj) {
        quill.on('text-change', () => {
            dotNetObj.invokeMethodAsync('OnContentChanged', quill.root.innerHTML);
        });
    }
}

window.getQuillHtml = () => { return quill.root.innerHTML; }
window.setQuillHtml = (htmlContent) => { quill.root.innerHTML = htmlContent; }
