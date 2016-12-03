import { NgModule }             from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { DashboardComponent }   from './dashboard/dashboard.component';
import { WebUiComponent }   from './webui/webui.component';
import { KeyblockComponent }   from './keyblock/keyblock.component';
import { LogComponent }   from './log/log.component';
import { NewcamdComponent }   from './newcamd/newcamd.component';
import { EpgComponent }   from './epg/epg.component';
import { TvheadendComponent }   from './tvheadend/tvheadend.component';

const routes: Routes = [
  { path: '', redirectTo: '/dashboard', pathMatch: 'full' },
  { path: 'dashboard',  component: DashboardComponent },
  { path: 'WebUi',  component: WebUiComponent },
  { path: 'Keyblock',  component: KeyblockComponent },
  { path: 'Log',  component: LogComponent },
  { path: 'Newcamd',  component: NewcamdComponent },
  { path: 'Epg',  component: EpgComponent },
  { path: 'Tvheadend',  component: TvheadendComponent },
  //{ path: 'detail/:id', component: HeroDetailComponent },
];

@NgModule({
  imports: [ RouterModule.forRoot(routes) ],
  exports: [ RouterModule ]
})
export class AppRoutingModule {}