"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __metadata = (this && this.__metadata) || function (k, v) {
    if (typeof Reflect === "object" && typeof Reflect.metadata === "function") return Reflect.metadata(k, v);
};
var core_1 = require('@angular/core');
var module_service_1 = require('./module.service');
var WebUiComponent = (function () {
    function WebUiComponent(moduleService) {
        this.moduleService = moduleService;
        this.module = {};
    }
    WebUiComponent.prototype.ngOnInit = function () {
        this.getModule();
    };
    ;
    WebUiComponent.prototype.getModule = function () {
        var _this = this;
        this.moduleService.getModule('WebUi').then(function (module) {
            _this.module = module;
        });
    };
    WebUiComponent = __decorate([
        core_1.Component({
            moduleId: module.id,
            selector: 'webui',
            templateUrl: 'webui.component.html'
        }), 
        __metadata('design:paramtypes', [module_service_1.ModuleService])
    ], WebUiComponent);
    return WebUiComponent;
}());
exports.WebUiComponent = WebUiComponent;
//# sourceMappingURL=webui.component.js.map