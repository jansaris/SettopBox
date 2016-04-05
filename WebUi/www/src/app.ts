import {autoinject} from "aurelia-framework";
import {Router} from "aurelia-router"
import {RouterConfiguration} from "aurelia-router"
import {HttpClient} from "aurelia-fetch-client";

@autoinject
export class App {
    constructor(private router: Router, private http: HttpClient) {
        http.configure(config => { config.withBaseUrl("/api/"); });
        this.http = http;
        this.router = router;
        this.router.configure(this.generateRouterConfiguration(null));
    }

    private generateRouterConfiguration(modules: IModule[]) : RouterConfiguration {
        var config = new RouterConfiguration();
        config.title = "SettopBox";
        var routes = [];
        if (modules) {
            for (var index in modules) {
                var module = modules[index];
                routes.push({
                    route: 'module/:id',
                    href: '#/module/' + module.Name,
                    moduleId: 'module',
                    nav: true,
                    title: module.Name
                });
            }
        } else {
            routes.push({ route: ['', 'home'], moduleId: 'home', nav: true, title: 'Welcome' });
        }

        config.map(routes);
        return config;
    }

    activate() {
        return this.http.fetch("module")
            .then(response => response.json()
            .then(modules => {
                this.router.configure(this.generateRouterConfiguration(<IModule[]>modules));
            }));
    }
}
