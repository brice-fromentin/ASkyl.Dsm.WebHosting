function scrollToTreeItem(treeItem) {
    setTimeout(() => treeItem.scrollIntoView({ block: 'center' }), 50);
}

function createSelectionObserver(childItem, onSelectionChange) {
    const observer = new MutationObserver((mutations) => {
        mutations.forEach((mutation) => {
            if (mutation.type === 'attributes' && mutation.attributeName === 'selected') {
                if (childItem.selected || childItem.hasAttribute('selected')) {
                    onSelectionChange();
                    observer.disconnect();
                }
            }
        });
    });

    observer.observe(childItem, { attributes: true, attributeFilter: ['selected'] });

    setTimeout(() => observer.disconnect(), 2000);

    return observer;
}

function selectChildItem(childId, parentId) {
    if (window.getSelection) {
        window.getSelection().removeAllRanges();
    }

    const parentItem = document.querySelector(`fluent-tree-item[id="${parentId}"]`);
    parentItem.expanded = true;

    setTimeout(() => {
        const waitForElement = () => {
            const childItem = parentItem.querySelector(`fluent-tree-item[id="${childId}"]`);

            if (childItem) {
                childItem.click();

                createSelectionObserver(childItem, () => scrollToTreeItem(childItem));
            }
            else {
                setTimeout(waitForElement, 100);
            }
        };
        
        waitForElement();
    }, 100);
}