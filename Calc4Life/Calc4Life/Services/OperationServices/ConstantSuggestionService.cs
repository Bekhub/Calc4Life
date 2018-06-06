using Calc4Life.Models;
using Calc4Life.Services.RepositoryServices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace Calc4Life.Services.OperationServices
{
    public class ConstantSuggestionService
    {
        IConstantsRepositoryService _constantsRepository;
        private List<Constant> _constants;
        private bool constantsUpdatedFlag;
        public ConstantSuggestionService()
        {
            _constantsRepository = App.Database; 
        }
        public IObservable<List<Constant>> SuggestionsObservable(IObservable<string> observable)
        {
            //var propertyChangedObservable = Observable.FromEventPattern<PropertyChangedEventArgs>(target, eventName);
            //var displayChangedObservable = propertyChangedObservable.Where(r => r.EventArgs.PropertyName == propertyName).Select(m => propertyName);
            var subject = new Subject<List<Constant>>();

            observable
                 .DistinctUntilChanged()
                     .Subscribe(async s =>
                     {
                         if (!constantsUpdatedFlag)
                         {
                             _constants = await _constantsRepository.GetItemsAsync();
                             _constants.ForEach(c => { if (c.Name.Count() > 27) c.Name = c.Name.Substring(0, 27) + "..."; });
                             constantsUpdatedFlag = true;
                         }
                         var consts = _constants?.Where(c => c.Value.ToString().StartsWith(s));
                         subject.OnNext(consts.ToList());
    
    
                     });
            return subject;
        }
    }
}
