import { Injectable, EventEmitter, Output } from '@angular/core';

@Injectable()
export class ErrorService {

  @Output() errorOccured = new EventEmitter();
  @Output() lastError: string = "null";

  constructor() {
  }

  handleError(error: any): Promise<any> {
      let message = error;
      if(error && error.statusText) message = error.statusText;
      else message = "Unkown error occured";
      
      this.lastError = message;
      //console.error('An error occurred', message); // for demo purposes only
      
      this.errorOccured.emit({value: message});
      return Promise.reject(message || error);
  }

}
