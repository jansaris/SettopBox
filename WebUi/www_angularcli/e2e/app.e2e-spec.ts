import { WwwAngularcliPage } from './app.po';

describe('www-angularcli App', function() {
  let page: WwwAngularcliPage;

  beforeEach(() => {
    page = new WwwAngularcliPage();
  });

  it('should display message saying app works', () => {
    page.navigateTo();
    expect(page.getParagraphText()).toEqual('app works!');
  });
});
