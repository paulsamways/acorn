export const getRequiredDomNode = (selectors: string): Element => {
  const domNode = document.querySelector(selectors);
  if (!domNode) {
    throw new Error(`DOM element with selectors '${selectors}' not found.`);
  }
  return domNode;
}
export const getRequiredDomData = <T>(selectors: string): T => {
  const domDataNode = getRequiredDomNode(selectors);
  // TODO: Add error handling for invalid JSON
  return JSON.parse(domDataNode.textContent ?? "") as T;
}
