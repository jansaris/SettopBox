import { Component, OnInit, Input  } from '@angular/core';
import { Setting } from '../models';

@Component({
  selector: 'app-setting',
  templateUrl: './setting.component.html',
  styleUrls: ['./setting.component.css']
})
export class SettingComponent implements OnInit {

  @Input()
  setting: Setting;

  constructor() { }

  ngOnInit() {
  }

}
