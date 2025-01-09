let resizeObserver;
let lastBreakpoint = '';

export function initializeResizeObserver(dotNetReference, element, largeMinWidth, mediumMinWidth) {
    resizeObserver = new ResizeObserver(entries => {
        for (let entry of entries) {
            const width = entry.contentRect.width;
            let breakpoint;

            if (width >= largeMinWidth) {
                breakpoint = 'Large';
            } else if (width >= mediumMinWidth) {
                breakpoint = 'Medium';
            } else {
                breakpoint = 'Small';
            }

            if (lastBreakpoint !== breakpoint) {
                lastBreakpoint = breakpoint;
                dotNetReference.invokeMethodAsync('OnBreakpointChanged', breakpoint);
            }
        }
    });

    resizeObserver.observe(element);
}

export function disposeResizeObserver(element) {
    if (resizeObserver) {
        resizeObserver.unobserve(element);
        resizeObserver = null;
    }
}