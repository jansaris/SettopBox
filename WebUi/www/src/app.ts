import {Router} from "aurelia-router"
import Aureliarouter = require("aurelia-router");

export class App {
    static inject() { return [Router]; }
    router: Router;
    constructor(router: Router) {
        this.router = router;
        var config = new Aureliarouter.RouterConfiguration();
        config.title = "SettopBox";
        config.map([
            { route: ['', 'home'], moduleId: 'home', nav: true, title: 'Welcome' }
        ]);
        this.router.configure(config);
    }
}
