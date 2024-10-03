window.ScrollToBottom = (elementName) => {
    element = document.getElementById(elementName);
    if (element)
        element.scrollTop = element.scrollHeight - element.clientHeight;
}