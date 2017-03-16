import { Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChange  } from '@angular/core';

@Component({
    selector: 'app-toggle-button',
    templateUrl: './toggle-button.component.html',
    styleUrls: ['./toggle-button.component.css']
})
export class ToggleButtonComponent implements OnInit, OnChanges {
    @Input()
    value: boolean;
    @Input()
    text: string;
    @Output() onChanged = new EventEmitter<boolean>();

    constructor() {
    }

    ngOnInit() {
    }

    ngOnChanges(changes: { [propKey: string]: SimpleChange }) {
        let log: string[] = [];
        for (let propName in changes) {
            let changedProp = changes[propName];
            if (changedProp.isFirstChange()) continue;
            if (this.value == changedProp.currentValue) continue;

            this.value = changedProp.currentValue;
        }
    }

    change() {
        this.value = !this.value;
        this.onChanged.emit(this.value);
    }
}
