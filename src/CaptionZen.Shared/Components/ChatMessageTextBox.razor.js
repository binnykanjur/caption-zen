export function initialize(textArea, maxHeight) {
    if (!textArea) return;

    textArea.dataset.maxHeight = maxHeight;

    resize(textArea);
}

export function resize(textArea) {
    if (!textArea) return;

    const maxHeight = parseInt(textArea.dataset.maxHeight) || 300;

    textArea.style.height = 'auto';
    const newHeight = Math.min(textArea.scrollHeight, maxHeight);
    textArea.style.height = `${newHeight}px`;

    textArea.style.overflowY = textArea.scrollHeight > maxHeight ? 'auto' : 'hidden';
}