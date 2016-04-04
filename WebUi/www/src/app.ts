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

    private generateRouterConfiguration(modules: IModule) : RouterConfiguration {
        var config = new RouterConfiguration();
        config.title = "SettopBox";
        var routes = [{ route: ['', 'home'], moduleId: 'home', nav: true, title: 'Welcome' }];
        if (modules) {
            //for (var module in modules) {
            //    var name = module.Name;
            //    routes.push({
            //            route: [module.Name],
            //            moduleId: module.Name,
            //            nav: true,
            //            title: module.Name
            //        }
            //    );
            //}
        }
        /**
         [

            //{ route: ['NewCamd', 'newcamd'], moduleId: 'newcamd', nav: true, title: 'NewCamd' }
        ]
         */
        config.map(routes);
        return config;
    }

    activate() {
        return this.http.fetch("module")
            .then(response => response.json()
            .then(modules => {
                this.router.configure(this.generateRouterConfiguration(<IModule>[]modules));
            }));
    }
}

interface IModule {
    Name: string;
    Enabled: boolean;
    Status: string;
}