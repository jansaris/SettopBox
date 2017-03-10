import { WwwNewPage } from './app.po';

describe('www-new App', () => {
  let page: WwwNewPage;

  beforeEach(() => {
    page = new WwwNewPage();
  });

  it('should display message saying app works', () => {
    page.navigateTo();
    expect(page.getParagraphText()).toEqual('app works!');
  });
});
